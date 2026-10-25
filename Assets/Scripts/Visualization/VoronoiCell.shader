// Unlit shader for one Voronoi cell: a flat color with the cell's perimeter drawn on top.
// The color is per renderer (_Color, set through a MaterialPropertyBlock), so every cell can share this one material.
//
// The mesh is a fan of triangles (center, edge start, edge end) and carries per vertex:
//   TEXCOORD0 (unused, edgeT, edgeDist)   edgeT: 0 at the fan's center, 1 along the cell's edge
//                                          edgeDist: distance from the center to that edge, in world units
// edgeT is linear across the triangle, so (1 - edgeT) * edgeDist is the true distance from a pixel to the edge.
// Each cell draws half of the line on its own side, so a border shared by two cells comes out at the full width.
Shader "Unlit/VoronoiCell"
{
    Properties
    {
        _Color     ("Cell Color", Color) = (1, 1, 1, 1)
        _EdgeColor ("Perimeter Color", Color) = (0.05, 0.05, 0.08, 1)
        _EdgeWidth ("Perimeter Width (world units)", Float) = 0.06
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed4 _EdgeColor;
            float  _EdgeWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float distToEdge = (1.0 - i.uv.y) * i.uv.z;
                float aa = max(fwidth(distToEdge), 1e-5);
                float edge = 1.0 - smoothstep(_EdgeWidth * 0.5 - aa, _EdgeWidth * 0.5 + aa, distToEdge);
                return fixed4(lerp(_Color.rgb, _EdgeColor.rgb, edge), 1);
            }
            ENDCG
        }
    }
}
