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
            public Vector2 texturePosition;
            public Vector2 size;
            public Vector2 bearing;
            public int advance;


            public Character(char id, Vector2 texturePosition, Vector2 size, Vector2 bearing, int advance)
            {
                this.id = id;
                this.texturePosition = texturePosition;
                this.size = size;
                this.bearing = bearing;
                this.advance = advance;
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

                        int id = 0, advance = 0;
                        float x = 0, y = 0, w = 0, h = 0, xoffset = 0, yoffset = 0;

                        // parse the contents of the line, looking for key words
                        for (int i = 0; i < split.Length; i++)
                        {
                            if (!split[i].Contains("=")) continue;
                            string code = split[i].Substring(0, split[i].IndexOf('='));
                            int value = int.Parse(split[i].Substring(split[i].IndexOf('=') + 1));

                            if (code == "id") id = value;
                            else if (code == "x") x = (float)value / FontTexture.Size.Width;
                            else if (code == "y") y = 1 - (float)value / FontTexture.Size.Height;
                            else if (code == "width") w = (float)value;
                            else if (code == "height")
                            {
                                h = (float)value;
                                this.Height = Math.Max(this.Height, value);
                            }
                            else if (code == "xoffset") xoffset = (float)value;
                            else if (code == "yoffset") yoffset = (float)value;
                            else if (code == "xadvance") advance = value;
                        }

                        // store this character into our dictionary (if it doesn't already exist)
                        Character c = new Character((char)id, new Vector2(x, y), new Vector2(w, h), new Vector2(xoffset, yoffset), advance);
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
                width += (int)characters[characters.ContainsKey(text[i]) ? text[i] : ' '].advance;

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
                width += (int)characters[characters.ContainsKey(text[i]) ? text[i] : ' '].advance;
                if (justification == Justification.Right) xpos = -width;
                else xpos = -width / 2;
            }

            for (uint i = 0; i < text.Length; i++)
            {
                // grab the character, replacing with ' ' if the character isn't loaded
                Character ch = characters[characters.ContainsKey(text[(int)i]) ? text[(int)i] : ' '];

                vertices[i * 4 + 0] = new Vector3(xpos + ch.bearing.X, Height - ch.bearing.Y, 0);
                vertices[i * 4 + 1] = new Vector3(xpos + ch.bearing.X, Height - (ch.bearing.Y + ch.size.Y), 0);
                vertices[i * 4 + 2] = new Vector3(xpos + ch.bearing.X + ch.size.X, Height - ch.bearing.Y, 0);
                vertices[i * 4 + 3] = new Vector3(xpos + ch.bearing.X + ch.size.X, Height - (ch.bearing.Y + ch.size.Y), 0);
                xpos += ch.advance;


                Vector2 bottomRight = ch.texturePosition
                    + new Vector2(ch.size.X / FontTexture.Size.Width, -ch.size.Y / FontTexture.Size.Height);

                uvs[i * 4 + 0] = new Vector2(ch.texturePosition.X, ch.texturePosition.Y);
                uvs[i * 4 + 1] = new Vector2(ch.texturePosition.X, bottomRight.Y);
                uvs[i * 4 + 2] = new Vector2(bottomRight.X, ch.texturePosition.Y);
                uvs[i * 4 + 3] = new Vector2(bottomRight.X, bottomRight.Y);

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
