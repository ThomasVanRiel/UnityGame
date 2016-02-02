Shader "Custom/OcclusionShader" {
	Properties {
		
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Geometry-100" }
		ColorMask 0
		Zwrite off

		Stencil {
			Ref 1
			Pass replace
		}

		pass{
			Blend Off
		}

	}
}
