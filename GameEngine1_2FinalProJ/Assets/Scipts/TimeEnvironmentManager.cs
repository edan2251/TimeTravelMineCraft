using UnityEngine;

[System.Serializable]
public struct AtmosphereData
{
    public string label; // 그냥 에디터 구분용 이름
    public Material skyboxMaterial; // 준비된 스카이박스

    [Header("Fog Settings")]
    public Color fogColor;
    public float fogDensity; // 0.01 ~ 0.05 추천

    [Header("Light Settings")]
    public Color directionalLightColor;
    public float lightIntensity;
}

public class TimeEnvironmentManager : MonoBehaviour
{
    [Header("References")]
    public Light mainDirectionalLight; // 씬에 있는 Directional Light 연결

    [Header("Settings")]
    public AtmosphereData morningAtmosphere;
    public AtmosphereData noonAtmosphere;
    public AtmosphereData nightAtmosphere;

    public void SetAtmosphere(MapType time)
    {
        AtmosphereData targetData = morningAtmosphere; // 기본값

        switch (time)
        {
            case MapType.Morning: targetData = morningAtmosphere; break;
            case MapType.Noon: targetData = noonAtmosphere; break;
            case MapType.Night: targetData = nightAtmosphere; break;
        }

        // 1. 스카이박스 변경
        RenderSettings.skybox = targetData.skyboxMaterial;

        // 2. 안개(Fog) 설정 변경
        RenderSettings.fog = true; // 안개 켜기
        RenderSettings.fogColor = targetData.fogColor;
        RenderSettings.fogDensity = targetData.fogDensity;

        // 3. 조명(Light) 설정 변경
        if (mainDirectionalLight != null)
        {
            mainDirectionalLight.color = targetData.directionalLightColor;
            mainDirectionalLight.intensity = targetData.lightIntensity;
        }

        // ★ 중요: 스카이박스가 바뀌었으니 환경광(Ambient Light) 재계산 요청
        DynamicGI.UpdateEnvironment();
    }
}