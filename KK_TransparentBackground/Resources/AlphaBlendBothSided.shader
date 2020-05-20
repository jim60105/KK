Shader "Custom/AlphaBlendBothSided"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)           // 物体纹理贴图乘该颜色得到物体颜色
		_MainTex("Alpha Texture", 2D) = "white" {}
		_AlphaScale("Alpha Scale", Range(0, 1)) = 1  // 控制透明度
	}
	SubShader
	{
		Tags{"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}

		Pass{
			Tags {"LightMode" = "ForwardBase"}

			Cull Front  // 先剔除正面渲染背面

			// 关闭深度写入（使透明物体后面的物体也能显示）
			ZWrite Off

			// 指定混合系数
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "Lighting.cginc"

			fixed4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed _AlphaScale;

			// 顶点着色器输入
			struct a2v {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
			};

			// 顶点着色器输出
			struct v2f {
				float4 pos : SV_POSITION;
				float3 worldNormal : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				float2 uv : TEXCOORD2;
			};

			// 顶点着色器
			v2f vert(a2v v) {
				v2f o;

				//模型空间到裁剪空间的坐标变化
				o.pos = UnityObjectToClipPos(v.vertex);

				o.worldNormal = UnityObjectToWorldNormal(v.normal);

				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

				return o;
			}

			// 片段着色器
			fixed4 frag(v2f i) : SV_Target{
				fixed3 worldNormal = normalize(i.worldNormal);
				fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));

				fixed4 texColor = tex2D(_MainTex, i.uv);

				fixed3 albedo = texColor.rgb * _Color.rgb;  // 物体albedo颜色

				fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * albedo;  // 环境光

				fixed3 diffuse = _LightColor0.rgb * albedo * max(dot(worldNormal, worldLightDir), 0);  // 漫反射

				fixed3 color = ambient + diffuse;

				return fixed4(color, texColor.a * _AlphaScale);
			}

			ENDCG
		}

		Pass{
			Tags {"LightMode" = "ForwardBase"}

			Cull Back  // 剔除背面渲染正面

			// 关闭深度写入（使透明物体后面的物体也能显示）
			ZWrite Off

			// 指定混合系数
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "Lighting.cginc"

			fixed4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed _AlphaScale;

			// 顶点着色器输入
			struct a2v {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
			};

			// 顶点着色器输出
			struct v2f {
				float4 pos : SV_POSITION;
				float3 worldNormal : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				float2 uv : TEXCOORD2;
			};

			// 顶点着色器
			v2f vert(a2v v) {
				v2f o;

				//模型空间到裁剪空间的坐标变化
				o.pos = UnityObjectToClipPos(v.vertex);

				o.worldNormal = UnityObjectToWorldNormal(v.normal);

				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

				return o;
			}

			// 片段着色器
			fixed4 frag(v2f i) : SV_Target{
				fixed3 worldNormal = normalize(i.worldNormal);
				fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));

				fixed4 texColor = tex2D(_MainTex, i.uv);

				fixed3 albedo = texColor.rgb * _Color.rgb;  // 物体albedo颜色

				fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * albedo;  // 环境光

				fixed3 diffuse = _LightColor0.rgb * albedo * max(dot(worldNormal, worldLightDir), 0);  // 漫反射

				fixed3 color = ambient + diffuse;

				return fixed4(color, texColor.a * _AlphaScale);
			}

			ENDCG
		}
	}
	FallBack "Transparent/VertexLit"
}