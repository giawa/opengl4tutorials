using System;
using System.Collections.Generic;
using Tao.FreeGlut;
using OpenGL;

namespace OpenGLTutorial11
{
    class Program
    {
        private static int width = 1280, height = 720;
        private static System.Diagnostics.Stopwatch watch;
        private static bool fullscreen = false;
        private static bool left, right, up, down;
        private static float theta = (float)Math.PI / 2, phi = (float)Math.PI / 2;

        private static ShaderProgram program;
        private static VBO<Vector3> flagVertices;
        private static VBO<Vector2> flagUVs;
        private static VBO<uint> flagTriangles;
        private static float flagTime;
        private static Texture flagTexture;

        private static BMFont font;
        private static ShaderProgram fontProgram;
        private static FontVAO information;

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
            Gl.Enable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Blend);

            // create our shader program
            program = new ShaderProgram(VertexShader, FragmentShader);

            // set up the projection and view matrix
            program.Use();
            program["projection_matrix"].SetValue(Matrix4.CreatePerspectiveFieldOfView(0.45f, (float)width / height, 0.1f, 1000f));
            program["view_matrix"].SetValue(Matrix4.LookAt(new Vector3(0, 0, 20), Vector3.Zero, new Vector3(0, 1, 0)));
            program["model_matrix"].SetValue(Matrix4.Identity);

            // load the flag texture
            flagTexture = new Texture("flag.png");

            // create the flag, which is just a plane with a certain number of segments
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<uint> triangles = new List<uint>();
            for (int x = 0; x < 40; x++)
            {
                for (int y = 0; y < 40; y++)
                {
                    vertices.Add(new Vector3((x - 20) / 5.0f, (y - 20) / 10.0f, 0));
                    uvs.Add(new Vector2(x / 39.0f, 1 - y / 39.0f));

                    if (y == 39 || x == 39) continue;

                    triangles.Add((uint)(x * 40 + y));
                    triangles.Add((uint)((x + 1) * 40 + y));
                    triangles.Add((uint)((x + 1) * 40 + y + 1));

                    triangles.Add((uint)(x * 40 + y));
                    triangles.Add((uint)((x + 1) * 40 + y + 1));
                    triangles.Add((uint)(x * 40 + y + 1));
                }
            }

            flagVertices = new VBO<Vector3>(vertices.ToArray());
            flagUVs = new VBO<Vector2>(uvs.ToArray());
            flagTriangles = new VBO<uint>(triangles.ToArray(), BufferTarget.ElementArrayBuffer);

            // load the bitmap font for this tutorial
            font = new BMFont("font24.fnt", "font24.png");
            fontProgram = new ShaderProgram(BMFont.FontVertexSource, BMFont.FontFragmentSource);

            fontProgram.Use();
            fontProgram["ortho_matrix"].SetValue(Matrix4.CreateOrthographic(width, height, 0, 1000));
            fontProgram["color"].SetValue(new Vector3(1, 1, 1));

            information = font.CreateString(fontProgram, "OpenGL  C#  Tutorial  11");

            watch = System.Diagnostics.Stopwatch.StartNew();

            Glut.glutMainLoop();
        }

        private static void OnClose()
        {
            flagVertices.Dispose();
            flagUVs.Dispose();
            flagTriangles.Dispose();
            flagTexture.Dispose();
            program.DisposeChildren = true;
            program.Dispose();
            fontProgram.DisposeChildren = true;
            fontProgram.Dispose();
            font.FontTexture.Dispose();
            information.Dispose();
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
            Gl.UseProgram(program.ProgramID);
            Gl.BindTexture(flagTexture);

            // calculate the camera position using some fancy polar co-ordinates
            Vector3 position = 20 * new Vector3((float)(Math.Cos(phi) * Math.Sin(theta)), (float)Math.Cos(theta), (float)(Math.Sin(phi) * Math.Sin(theta)));
            Vector3 upVector = ((theta % (Math.PI * 2)) > Math.PI) ? new Vector3(0, 1, 0) : new Vector3(0, -1, 0);
            program["view_matrix"].SetValue(Matrix4.LookAt(position, Vector3.Zero, upVector));

            program["time"].SetValue(flagTime);
            flagTime += deltaTime;

            Gl.BindBufferToShaderAttribute(flagVertices, program, "vertexPosition");
            Gl.BindBufferToShaderAttribute(flagUVs, program, "vertexUV");
            Gl.BindBuffer(flagTriangles);

            Gl.DrawElements(BeginMode.Triangles, flagTriangles.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);

            // bind the font program as well as the font texture
            Gl.UseProgram(fontProgram.ProgramID);
            Gl.BindTexture(font.FontTexture);

            // build this string every frame, since theta and phi can change
            FontVAO vao = font.CreateString(fontProgram, string.Format("Theta:   {0:0.000},  Phi:   {1:0.000},  Time:   {2:0.000}", theta, phi, flagTime), BMFont.Justification.Right);
            vao.Position = new Vector2(width / 2 - 10, height / 2 - font.Height - 10);
            vao.Draw();
            vao.Dispose();

            // draw the tutorial information, which is static
            information.Draw();

            Glut.glutSwapBuffers();
        }

        private static void OnReshape(int width, int height)
        {
            Program.width = width;
            Program.height = height;

            Gl.UseProgram(program.ProgramID);
            program["projection_matrix"].SetValue(Matrix4.CreatePerspectiveFieldOfView(0.45f, (float)width / height, 0.1f, 1000f));

            Gl.UseProgram(fontProgram.ProgramID);
            fontProgram["ortho_matrix"].SetValue(Matrix4.CreateOrthographic(width, height, 0, 1000));

            information.Position = new Vector2(-width / 2 + 10, height / 2 - font.Height - 10);
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
uniform float time;

void main(void)
{
    uv = vertexUV;

    float displace = sin(time * 2 + vertexPosition.x) / 3;
    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(vertexPosition.x, vertexPosition.y, displace, 1);
}
";

        public static string FragmentShader = @"
#version 130

uniform sampler2D texture;

in vec2 uv;

out vec4 fragment;

void main(void)
{
    fragment = vec4(texture2D(texture, uv).xyz, 1);
}
";
    }
}
