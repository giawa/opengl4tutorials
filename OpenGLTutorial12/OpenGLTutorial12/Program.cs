using System;
using System.Collections.Generic;
using Tao.FreeGlut;
using OpenGL;

namespace OpenGLTutorial12
{
    class Program
    {
        private static int width = 1280, height = 720;
        private static System.Diagnostics.Stopwatch watch;
        private static bool fullscreen = false, rainbow = false;

        private static ShaderProgram program;
        private static Texture particleTexture;
        private static VBO<Vector3> particleVertices;
        private static VBO<Vector3> particleColors;
        private static VBO<uint> particlePoints;

        private static List<Particle> particles = new List<Particle>();
        private static int particleCount = 2000;
        private static Vector3[] particlePositions = new Vector3[particleCount];
        private static Random generator = new Random();

        private static BMFont font;
        private static ShaderProgram fontProgram;
        private static FontVAO information;

        static void Main(string[] args)
        {
            Glut.glutInit();
            Glut.glutInitDisplayMode(Glut.GLUT_DOUBLE | Glut.GLUT_DEPTH | Glut.GLUT_MULTISAMPLE);   // multisampling makes things beautiful!
            Glut.glutInitWindowSize(width, height);
            Glut.glutCreateWindow("OpenGL Tutorial");

            Glut.glutIdleFunc(OnRenderFrame);
            Glut.glutDisplayFunc(OnDisplay);

            Glut.glutKeyboardFunc(OnKeyboardDown);
            Glut.glutKeyboardUpFunc(OnKeyboardUp);

            Glut.glutCloseFunc(OnClose);
            Glut.glutReshapeFunc(OnReshape);

            // enable blending and set to accumulate the star colors
            Gl.Enable(EnableCap.Blend);
            Gl.Enable(EnableCap.ProgramPointSize);
            Gl.Enable(EnableCap.Multisample);
            Gl.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);

            // create our shader program
            program = new ShaderProgram(VertexShader, FragmentShader);

            // set up the projection and view matrix
            program.Use();
            program["projection_matrix"].SetValue(Matrix4.CreatePerspectiveFieldOfView(0.45f, (float)width / height, 0.1f, 1000f));
            program["view_matrix"].SetValue(Matrix4.LookAt(new Vector3(0, 0, 20), Vector3.Zero, new Vector3(0, 1, 0)));
            program["model_matrix"].SetValue(Matrix4.Identity);
            program["static_colors"].SetValue(false);

            // load the particle texture
            particleTexture = new Texture("star.bmp");

            // set up the particlePoints VBO, which will stay constant
            uint[] points = new uint[particleCount];
            for (uint i = 0; i < points.Length; i++) points[i] = i;
            particlePoints = new VBO<uint>(points, BufferTarget.ElementArrayBuffer);

            // set up the particleColors, which we'll just keep static
            Vector3[] colors = new Vector3[particleCount];
            for (int i = 0; i < colors.Length; i++) colors[i] = new Vector3((float)generator.NextDouble(), (float)generator.NextDouble(), (float)generator.NextDouble());
            particleColors = new VBO<Vector3>(colors);

            // build up our first batch of 1000 particles and 1000 static colors
            for (int i = 0; i < particleCount; i++) particles.Add(new Particle(Vector3.Zero, 0));

            // load the bitmap font for this tutorial
            font = new BMFont("font24.fnt", "font24.png");
            fontProgram = new ShaderProgram(BMFont.FontVertexSource, BMFont.FontFragmentSource);

            fontProgram.Use();
            fontProgram["ortho_matrix"].SetValue(Matrix4.CreateOrthographic(width, height, 0, 1000));
            fontProgram["color"].SetValue(new Vector3(1, 1, 1));

            information = font.CreateString(fontProgram, "OpenGL  C#  Tutorial  12");

            watch = System.Diagnostics.Stopwatch.StartNew();

            Glut.glutMainLoop();
        }

        private static void OnClose()
        {
            particleTexture.Dispose();
            particleVertices.Dispose();
            particleColors.Dispose();
            particlePoints.Dispose();
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

        private class Particle
        {
            public float Life;
            public Vector3 Position;
            public Vector3 Direction;

            public Particle(Vector3 origin, float life = 2)
            {
                Position = origin;
                Direction = new Vector3((float)generator.NextDouble() * 2 - 1, 3 + (float)generator.NextDouble(), (float)generator.NextDouble() * 2 - 1);
                Life = life + (float)generator.NextDouble() * 2;
            }

            public void Update(float delta)
            {
                Direction += delta * new Vector3(0, -2, 0);
                Position += Direction * delta;
                Life -= delta;
            }
        }

        private static void OnRenderFrame()
        {
            watch.Stop();
            float deltaTime = (float)watch.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency;
            watch.Restart();

            // set up the viewport and clear the previous depth and color buffers
            Gl.Viewport(0, 0, width, height);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // make sure the shader program and texture are being used
            Gl.UseProgram(program.ProgramID);
            Gl.BindTexture(particleTexture);

            // update our particle list
            for (int i = 0; i < particles.Count; i++)
            {
                particles[i].Update(deltaTime);
                if (particles[i].Life < 0) particles[i] = new Particle(Vector3.Zero);
                particlePositions[i] = particles[i].Position;
            }

            // delete our previous particle positions (if applicable) and then create a new VBO
            if (particleVertices != null) particleVertices.Dispose();
            particleVertices = new VBO<Vector3>(particlePositions);

            // bind the VBOs to their shader attributes
            Gl.BindBufferToShaderAttribute(particleVertices, program, "vertexPosition");
            Gl.BindBufferToShaderAttribute(particleColors, program, "vertexColor");
            Gl.BindBuffer(particlePoints);

            // enable point sprite mode (which enables the gl_PointCoord value)
            Gl.Enable(EnableCap.PointSprite);
            Gl.DrawElements(BeginMode.Points, particlePoints.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);
            Gl.Disable(EnableCap.PointSprite);

            // bind the font program as well as the font texture
            Gl.UseProgram(fontProgram.ProgramID);
            Gl.BindTexture(font.FontTexture);

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
            if (key == 27) Glut.glutLeaveMainLoop();
        }

        private static void OnKeyboardUp(byte key, int x, int y)
        {
            if (key == 'f')
            {
                fullscreen = !fullscreen;
                if (fullscreen) Glut.glutFullScreen();
                else
                {
                    Glut.glutPositionWindow(0, 0);
                    Glut.glutReshapeWindow(1280, 720);
                }
            }
            else if (key == 'r')
            {
                rainbow = !rainbow;
                program.Use();
                program["static_colors"].SetValue(rainbow);
            }
        }

        public static string VertexShader = @"
#version 130

in vec3 vertexPosition;
in vec3 vertexColor;

out vec3 color;

uniform mat4 projection_matrix;
uniform mat4 view_matrix;
uniform mat4 model_matrix;
uniform bool static_colors;

void main(void)
{
    color = (static_colors ? vertexColor : mix(vec3(0, 0, 1), vec3(0.7, 0, 1), clamp(vertexPosition.y / 2, 0, 1)));

    gl_PointSize = clamp(10 + vertexPosition.y * 5, 0, 10);

    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(vertexPosition.xyz, 1);
}
";

        public static string FragmentShader = @"
#version 130

uniform sampler2D texture;

in vec3 color;

out vec4 fragment;

void main(void)
{
    fragment = vec4(color * texture2D(texture, gl_PointCoord).xyz, 1);
}
";
    }
}
