#version 110

#pragma vertex
            void main()
{
    gl_Position =  gl_Vertex;
}

		#pragma fragment

		//#define PHONG_LIGHT
		
		//#pragma include B:\Sharp.Engine\Sharp\Content\LightProcess.inc

		void main()
{
    gl_FragColor = vec4(1.0,1.0, 1.0, 1.0);
}