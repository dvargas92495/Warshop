Shader "Custom/GlowShader" {
    Properties
    {
        _MyTexture ("My texture", 2D) = "white" {}
        _MyNormalMap ("My normal map", 2D) = "bump" {}  // Grey

        _MyInt ("My integer", Int) = 2
        _MyFloat ("My float", Float) = 1.5
        _MyRange ("My range", Range(0.0, 1.0)) = 0.5

        _MyColor ("My colour", Color) = (1, 0, 0, 1)    // (R, G, B, A)
        _MyVector ("My Vector4", Vector) = (0, 0, 0, 0) // (x, y, z, w)
    }

    SubShader
    {
		Tags
		{
			"Queue" = "Geometry"
			"RenderType" = "Opaque"
		}
		CGPROGRAM
		sampler2D _MyTexture;
		sampler2D _MyNormalMap;

		int _MyInt;
		float _MyFloat;
		float _MyRange;
		half4 _MyColor;
		float4 _MyVector;
		// Cg / HLSL code of the shader
		// ...
		ENDCG
    }   
}