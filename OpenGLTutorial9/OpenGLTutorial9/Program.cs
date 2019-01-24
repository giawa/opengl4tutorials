using System;
using System.Collections.Generic;
using Tao.FreeGlut;
using OpenGL;

namespace OpenGLTutorial9
{
    class Program
    {
        private static int width = 1280, height = 720;
        private static ShaderProgram program;
        private static VBO<Vector3> star;
        private static VBO<Vector2> starUV;
        private static VBO<uint> starQuads;
        private static Texture starTexture;
        private static System.Diagnostics.Stopwatch watch;
        private static bool fullscreen = false;
        private static bool left, right, up, down;
        private static List<Star> stars = new List<Star>();
        private static Random generator = new Random(Environment.TickCount);
        private static float theta = (float)Math.PI / 2, phi = (float)Math.PI / 2;

        private class Star
        {
            public float angle;
            public float dist;
            public Vector3 color;

            public Star(float Angle, float Distance, Vector3 Color)
            {
                this.angle = Angle;
                this.dist = Distance;
                this.color = Color;
            }
        }

        static void Main(string[] args)
        {
            Glut.glutInit();
            Glut.glutInitDisplayMode(Glut.GLUT_DOUBLE | Glut.GLUT_DEPTH);
            Glut.glutInitWindowSize(width, height);
            Glut.glutCreateWindow("OpenGL Tutorial");

            Glut.glutIdleFunc(OnRenderFrame);
            Glut.glutDisplayFunc(OnDisplay);

            Glut.glutKeyboardFunc(OnKeyboardDown);
            Glut.glutKeyboardUpFunc(OnKeyboardUp);

            Glut.glutCloseFunc(OnClose);
            Glut.glutReshapeFunc(OnReshape);

            // enable blending and set to accumulate the star colors
            Gl.Disable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);

            // create our shader program
            program = new ShaderProgram(VertexShader, FragmentShader);

            // set up the projection and view matrix
            program.Use();
            program["projection_matrix"].SetValue(Matrix4.CreatePerspectiveFieldOfView(0.45f, (float)width / height, 0.1f, 1000f));
            program["view_matrix"].SetValue(Matrix4.LookAt(new Vector3(0, 0, 20), Vector3.Zero, new Vector3(0, 1, 0)));

            // load the star texture
            starTexture = new Texture("star.bmp");

            // each star is simply a quad
            star = new VBO<Vector3>(new Vector3[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) });
            starUV = new VBO<Vector2>(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            starQuads = new VBO<uint>(new uint[] { 0, 1, 2, 0, 2, 3 }, BufferTarget.ElementArrayBuffer);

            // create 50 stars for this tutorial
            int numStars = 50;
            for (int i = 0; i < numStars; i++)
            {
                stars.Add(new Star(0, (float)i / numStars * 4f, new Vector3((float)generator.NextDouble(), (float)generator.NextDouble(), (float)generator.NextDouble())));
            }

            watch = System.Diagnostics.Stopwatch.StartNew();

            Glut.glutMainLoop();
        }

        private static void OnClose()
        {
            star.Dispose();
            starUV.Dispose();
            starQuads.Dispose();
            starTexture.Dispose();
            program.DisposeChildren = true;
            program.Dispose();
        }

        private static void OnDisplay()
        {

        }

        private static void OnRenderFrame()
        {
            watch.Stop();
            float deltaTime = (float)watch.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency;
            watch.Restart();

            // perform rotation of the scene depending on keyboard input
            if (right) phi += deltaTime;
            if (left) phi -= deltaTime;
            if (up) theta += deltaTime;
            if (down) theta -= deltaTime;
            if (theta < 0) theta += (float)Math.PI * 2;

            // set up the viewport and clear the previous depth and color buffers
            Gl.Viewport(0, 0, width, height);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // make sure the shader program and texture are being used
            Gl.UseProgram(program);
            Gl.BindTexture(starTexture);

            // calculate the camera position using some fancy polar co-ordinates
            Vector3 position = 20 * new Vector3((float)(Math.Cos(phi) * Math.Sin(theta)), (float)Math.Cos(theta), (float)(Math.Sin(phi) * Math.Sin(theta)));
            Vector3 upVector = ((theta % (Math.PI * 2)) > Math.PI) ? new Vector3(0, 1, 0) : new Vector3(0, -1, 0);
            program["view_matrix"].SetValue(Matrix4.LookAt(position, Vector3.Zero, upVector));

            // loop through the stars, drawing each one
            for (int i = 0; i < stars.Count; i++)
            {
                // set the position and color of this star
                program["model_matrix"].SetValue(Matrix4.CreateTranslation(new Vector3(stars[i].dist, 0, 0)) * Matrix4.CreateRotationZ(stars[i].angle));
                program["color"].SetValue(stars[i].color);

                Gl.BindBufferToShaderAttribute(star, program, "vertexPosition");
                Gl.BindBufferToShaderAttribute(starUV, program, "vertexUV");
                Gl.BindBuffer(starQuads);

                Gl.DrawElements(BeginMode.Triangles, starQuads.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);

                // update the position of the star
                stars[i].angle += (float)i / stars.Count * deltaTime * 2;
                stars[i].dist -= 0.2f * deltaTime;

                // if we've reached the center then move this star outwards and give it a new color
                if (stars[i].dist < 0f)
                {
                    stars[i].dist += 5f;
                    stars[i].color = new Vector3((float)generator.NextDouble(), (float)generator.NextDouble(), (float)generator.NextDouble());
                }
            }

            Glut.glutSwapBuffers();
        }

        private static void OnReshape(int width, int height)
        {
            Program.width = width;
            Program.height = height;

            program.Use();
            program["projection_matrix"].SetValue(Matrix4.CreatePerspectiveFieldOfView(0.45f, (float)width / height, 0.1f, 1000f));
        }

        private static void OnKeyboardDown(byte key, int x, int y)
        {
            if (key == 'w') up = true;
            else if (key == 's') down = true;
            else if (key == 'd') right = true;
            else if (key == 'a') left = true;
            else if (key == 27) Glut.glutLeaveMainLoop();
        }

        private static void OnKeyboardUp(byte key, int x, int y)
        {
            if (key == 'w') up = false;
            else if (key == 's') down = false;
            else if (key == 'd') right = false;
            else if (key == 'a') left = false;
            else if (key == 'f')
            {
                fullscreen = !fullscreen;
                if (fullscreen) Glut.glutFullScreen();
                else
                {
                    Glut.glutPositionWindow(0, 0);
                    Glut.glutReshapeWindow(1280, 720);
                }
            }
        }

        public static string VertexShader = @"
#version 130

in vec3 vertexPosition;
in vec2 vertexUV;

out vec2 uv;

uniform mat4 projection_matrix;
uniform mat4 view_matrix;
uniform mat4 model_matrix;

void main(void)
{
    uv = vertexUV;

    gl_Position = projection_matrix * (view_matrix * model_matrix * vec4(0, 0, 0, 1) + vec4(vertexPosition.x, vertexPosition.y, vertexPosition.z, 0));
}
";

        public static string FragmentShader = @"
#version 130

uniform sampler2D texture;
uniform vec3 color;

in vec2 uv;

out vec4 fragment;

void main(void)
{
    fragment = vec4(color * texture2D(texture, uv).xyz, 1);
}
";
    }
}
