using System;
using System.Collections.Generic;
using System.IO;

using OpenGL;

namespace OpenGLTutorial15
{
    public class FontVAO : IDisposable
    {
        private ShaderProgram program;
        private VBO<Vector3> vertices;
        private VBO<Vector2> uvs;
        private VBO<uint> triangles;

        public Vector2 Position { get; set; }

        public FontVAO(ShaderProgram program, VBO<Vector3> vertices, VBO<Vector2> uvs, VBO<uint> triangles)
        {
            this.program = program;
            this.vertices = vertices;
            this.uvs = uvs;
            this.triangles = triangles;
        }

        public void Draw()
        {
            if (vertices == null) return;

            program.Use();
            program["model_matrix"].SetValue(Matrix4.CreateTranslation(new Vector3(Position.X, Position.Y, 0)));

            Gl.BindBufferToShaderAttribute(vertices, program, "vertexPosition");
            Gl.BindBufferToShaderAttribute(uvs, program, "vertexUV");
            Gl.BindBuffer(triangles);

            Gl.DrawElements(BeginMode.Triangles, triangles.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);
        }

        public void Dispose()
        {
            vertices.Dispose();
            uvs.Dispose();
            triangles.Dispose();

            vertices = null;
        }
    }

    /// <summary>
    /// The BMFont class can be used to load both the texture and data files associated with
    /// the free BMFont tool (http://www.angelcode.com/products/bmfont/)
    /// This tool allows 
    /// </summary>
    public class BMFont
    {
        /// <summary>
        /// Stores the ID, height, width and UV information for a single bitmap character
        /// as exported by the BMFont tool.
        /// </summary>
        private struct Character
        {
            public char id;
            public float x1;
            public float y1;
            public float x2;
            public float y2;
            public float width;
            public float height;

            public Character(char _id, float _x1, float _y1, float _x2, float _y2, float _w, float _h)
            {
                id = _id;
                x1 = _x1;
                y1 = _y1;
                x2 = _x2;
                y2 = _y2;
                width = _w;
                height = _h;
            }
        }

        /// <summary>
        /// Text justification to be applied when creating the VAO representing some text.
        /// </summary>
        public enum Justification
        {
            Left,
            Center,
            Right
        }

        /// <summary>
        /// The font texture associated with this bitmap font.
        /// </summary>
        public Texture FontTexture { get; private set; }

        private Dictionary<char, Character> characters = new Dictionary<char, Character>();

        /// <summary>
        /// The height (in pixels) of this bitmap font.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Loads both a font descriptor table and the associated texture as exported by BMFont.
        /// </summary>
        /// <param name="descriptorPath">The path to the font descriptor table.</param>
        /// <param name="texturePath">The path to the font texture.</param>
        public BMFont(string descriptorPath, string texturePath)
        {
            this.FontTexture = new Texture(texturePath);

            using (StreamReader stream = new StreamReader(descriptorPath))
            {
                while (!stream.EndOfStream)
                {
                    string line = stream.ReadLine();
                    if (line.StartsWith("char"))
                    {
                        // chars lets the program know how many characters are in this file, ignore it
                        if (line.StartsWith("chars")) continue;

                        // split up the different entries on this line to be parsed
                        string[] split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        int id = 0;
                        float x1 = 0, y1 = 0, x2 = 0, y2 = 0, w = 0, h = 0;

                        // parse the contents of the line, looking for key words
                        for (int i = 0; i < split.Length; i++)
                        {
                            if (!split[i].Contains("=")) continue;
                            string code = split[i].Substring(0, split[i].IndexOf('='));
                            int value = int.Parse(split[i].Substring(split[i].IndexOf('=') + 1));

                            if (code == "id") id = value;
                            else if (code == "x") x1 = (float)value / FontTexture.Size.Width;
                            else if (code == "y") y1 = 1 - (float)value / FontTexture.Size.Height;
                            else if (code == "width")
                            {
                                w = (float)value;
                                x2 = x1 + w / FontTexture.Size.Width;
                            }
                            else if (code == "height")
                            {
                                h = (float)value;
                                y2 = y1 - h / FontTexture.Size.Height;
                                this.Height = Math.Max(this.Height, value);
                            }
                        }

                        // store this character into our dictionary (if it doesn't already exist)
                        Character c = new Character((char)id, x1, y1, x2, y2, w, h);
                        if (!characters.ContainsKey(c.id)) characters.Add(c.id, c);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the width (in pixels) of a string of text using the current loaded font.
        /// </summary>
        /// <param name="text">The string of text to measure the width of.</param>
        /// <returns>The width (in pixels) of the provided text.</returns>
        public int GetWidth(string text)
        {
            int width = 0;

            for (int i = 0; i < text.Length; i++)
                width += (int)characters[characters.ContainsKey(text[i]) ? text[i] : ' '].width;

            return width;
        }

        public FontVAO CreateString(ShaderProgram program, string text, Justification justification = Justification.Left)
        {
            Vector3[] vertices = new Vector3[text.Length * 4];
            Vector2[] uvs = new Vector2[text.Length * 4];
            uint[] indices = new uint[text.Length * 6];

            int xpos = 0, width = 0;

            // calculate the initial x position depending on the justification
            if (justification != Justification.Left)
            {
                for (int i = 0; i < text.Length; i++)
                    width += (int)characters[characters.ContainsKey(text[i]) ? text[i] : ' '].width;
                if (justification == Justification.Right) xpos = -width;
                else xpos = -width / 2;
            }

            for (uint i = 0; i < text.Length; i++)
            {
                // grab the character, replacing with ' ' if the character isn't loaded
                Character ch = characters[characters.ContainsKey(text[(int)i]) ? text[(int)i] : ' '];

                vertices[i * 4 + 0] = new Vector3(xpos, ch.height, 0);
                vertices[i * 4 + 1] = new Vector3(xpos, 0, 0);
                vertices[i * 4 + 2] = new Vector3(xpos + ch.width, ch.height, 0);
                vertices[i * 4 + 3] = new Vector3(xpos + ch.width, 0, 0);
                xpos += (int)ch.width;

                uvs[i * 4 + 0] = new Vector2(ch.x1, ch.y1);
                uvs[i * 4 + 1] = new Vector2(ch.x1, ch.y2);
                uvs[i * 4 + 2] = new Vector2(ch.x2, ch.y1);
                uvs[i * 4 + 3] = new Vector2(ch.x2, ch.y2);

                indices[i * 6 + 0] = i * 4 + 2;
                indices[i * 6 + 1] = i * 4 + 0;
                indices[i * 6 + 2] = i * 4 + 1;
                indices[i * 6 + 3] = i * 4 + 3;
                indices[i * 6 + 4] = i * 4 + 2;
                indices[i * 6 + 5] = i * 4 + 1;
            }

            // Create the vertex buffer objects and then create the array object
            return new FontVAO(program, new VBO<Vector3>(vertices), new VBO<Vector2>(uvs), new VBO<uint>(indices, BufferTarget.ElementArrayBuffer));
        }

        public static string FontVertexSource = @"
#version 130

uniform mat4 model_matrix;
uniform mat4 ortho_matrix;

in vec3 vertexPosition;
in vec2 vertexUV;

out vec2 uv;

void main(void)
{
    uv = vertexUV;
    gl_Position = ortho_matrix * model_matrix * vec4(vertexPosition, 1);
}";

        public static string FontFragmentSource = @"
#version 130

uniform sampler2D texture;
uniform vec3 color;

in vec2 uv;

out vec4 fragment;

void main(void)
{
    vec4 texel = texture2D(texture, uv);
    fragment = vec4(texel.rgb * color, texel.a);
}
";
    }
}
