﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using Takochu.fmt;
using System.Globalization;

namespace Takochu.rnd.BmdRendererSys
{
    public class ShaderSetting
    {
        private static readonly string[] GenSrc = {
            "normalize(gl_Vertex)",
            "vec4(gl_Normal,1.0)",
            "argh",
            "argh",
            "gl_MultiTexCoord0",
            "gl_MultiTexCoord1",
            "gl_MultiTexCoord2",
            "gl_MultiTexCoord3",
            "gl_MultiTexCoord4",
            "gl_MultiTexCoord5",
            "gl_MultiTexCoord6",
            "gl_MultiTexCoord7"
        };
        private static readonly string[] outputregs = {
            "rprev",
            "r0",
            "r1",
            "r2"
        };
        private static readonly string[] c_inputregs = {
            "truncc3(rprev.rgb)",
            "truncc3(rprev.aaa)",
            "truncc3(r0.rgb)",
            "truncc3(r0.aaa)",
            "truncc3(r1.rgb)",
            "truncc3(r1.aaa)",
            "truncc3(r2.rgb)",
            "truncc3(r2.aaa)",
            "texcolor.rgb",
            "texcolor.aaa",
            "rascolor.rgb",
            "rascolor.aaa",
            "vec3(1.0,1.0,1.0)",
            "vec3(0.5,0.5,0.5)",
            "konst.rgb",
            "vec3(0.0,0.0,0.0)"
        };
        private static readonly string[] c_inputregsD = {
            "rprev.rgb",
            "rprev.aaa",
            "r0.rgb",
            "r0.aaa",
            "r1.rgb",
            "r1.aaa",
            "r2.rgb",
            "r2.aaa",
            "texcolor.rgb",
            "texcolor.aaa",
            "rascolor.rgb",
            "rascolor.aaa",
            "vec3(1.0,1.0,1.0)",
            "vec3(0.5,0.5,0.5)",
            "konst.rgb",
            "vec3(0.0,0.0,0.0)"
        };
        private static readonly string[] c_konstsel = {
            "vec3(1.0,1.0,1.0)",
            "vec3(0.875,0.875,0.875)",
            "vec3(0.75,0.75,0.75)",
            "vec3(0.625,0.625,0.625)",
            "vec3(0.5,0.5,0.5)",
            "vec3(0.375,0.375,0.375)",
            "vec3(0.25,0.25,0.25)",
            "vec3(0.125,0.125,0.125)",
            "",
            "",
            "",
            "",
            "k0.rgb",
            "k1.rgb",
            "k2.rgb",
            "k3.rgb",
            "k0.rrr",
            "k1.rrr",
            "k2.rrr",
            "k3.rrr",
            "k0.ggg",
            "k1.ggg",
            "k2.ggg",
            "k3.ggg",
            "k0.bbb",
            "k1.bbb",
            "k2.bbb",
            "k3.bbb",
            "k0.aaa",
            "k1.aaa",
            "k2.aaa",
            "k3.aaa"
        };

        private static readonly string[] a_inputregs = { 
            "truncc1(rprev.a)", 
            "truncc1(r0.a)", 
            "truncc1(r1.a)", 
            "truncc1(r2.a)",
            "texcolor.a", 
            "rascolor.a", 
            "konst.a", 
            "0.0" 
        };

        private static readonly string[] a_inputregsD = {
            "rprev.a", 
            "r0.a", 
            "r1.a", 
            "r2.a",
            "texcolor.a", 
            "rascolor.a", 
            "konst.a", 
            "0.0" 
        };

        private static readonly string[] a_konstsel = {
            "1.0", 
            "0.875", 
            "0.75", 
            "0.625", 
            "0.5", 
            "0.375", 
            "0.25", 
            "0.125",
            "", 
            "", 
            "", 
            "", 
            "", 
            "", 
            "", 
            "",
            "k0.r", 
            "k1.r", 
            "k2.r", 
            "k3.r", 
            "k0.g", 
            "k1.g", 
            "k2.g", 
            "k3.g",
            "k0.b", 
            "k1.b", 
            "k2.b", 
            "k3.b", 
            "k0.a", 
            "k1.a", 
            "k2.a", 
            "k3.a" 
        };

        private static readonly string[] tevbias = { 
            "0.0", 
            "0.5", 
            "-0.5" 
        };

        private static readonly string[] tevscale = { 
            "1.0", 
            "2.0", 
            "4.0", 
            "0.5" 
        };

        private static readonly string[] alphacompare = { 
            "{0} != {0}", 
            "{0} < {1}", 
            "{0} == {1}", 
            "{0} <= {1}", 
            "{0} > {1}", 
            "{0} != {1}", 
            "{0} >= {1}", 
            "{0} == {0}" 
        };

        // string[] alphacombine = { "all(bvec2({0},{1}))", "any(bvec2({0},{1}))", "any(bvec2(all(bvec2({0},!{1})),all(bvec2(!{0},{1}))))", "any(bvec2(all(bvec2({0},{1})),all(bvec2(!{0},!{1}))))" };
    
        private static readonly string[] alphacombine = { 
            "({0}) && ({1})", 
            "({0}) || ({1})", 
            "(({0}) && (!({1}))) || ((!({0})) && ({1}))", 
            "(({0}) && ({1})) || ((!({0})) && (!({1})))" 
        };

        // yes, oldstyle shaders
        // I would use version 130 or above but there are certain
        // of their new designs I don't agree with. Namely, what's
        // up with removing texture coordinates. That's just plain
        // retarded.

        private int _materialID;
        private int _success;
        private CultureInfo _forceusa;
        private BmdRenderer.Shader[] _shaders;
        private BmdRenderer.Shader _shader;
        private StringBuilder _vertex,_fragment;
        private BMD.Material _material;

        private bool isCompiled 
        {
            get
            {
                return (_success == (int)All.True);
            }
        }

        public ShaderSetting(BMD bmdmodel , int materialID) 
        {
            _materialID  = materialID;
            _forceusa    = new CultureInfo("en-US");
            _material    = bmdmodel.Materials[_materialID];
            _vertex      = new StringBuilder();
            _fragment    = new StringBuilder();
        }

        public void GenerateShader(ref BmdRenderer.Shader[] shaders) 
        {
            _shaders = shaders;
            _shader = _shaders[_materialID];
            SetVertex();
            SetFragment();
            GenerateShaderProgram();
            shaders[_materialID] = _shader;
        }

        private void SetVertex() 
        {
            VertexStringJoint();
            GenerateAndCompile_Shader(out _shader.VertexShader, ShaderType.VertexShader);
        }

        private void SetFragment() 
        {
            FragmentStringJoint();
            GenerateAndCompile_Shader(out _shader.FragmentShader, ShaderType.FragmentShader);
        }

        private void GenerateShaderProgram() 
        {
            _shader.Program = GL.CreateProgram();

            GL.AttachShader(_shader.Program, _shader.VertexShader);
            GL.AttachShader(_shader.Program, _shader.FragmentShader);

            GL.LinkProgram(_shader.Program);
            GL.GetProgram(_shader.Program, ProgramParameter.LinkStatus, out _success);
            if (_success == 0)
            {
                string log = GL.GetProgramInfoLog(_shader.Program);
                throw new Exception("!Failed to link shader program: " + log);
                // TODO: better error reporting/logging?
            }

            //debugshaders += "-----------------------------------------------------------\n" + frag.ToString();
        }

        private void VertexStringJoint() 
        {
            _vertex.AppendLine("#version 120");
            _vertex.AppendLine("");
            _vertex.AppendLine("void main()");
            _vertex.AppendLine("{");
            _vertex.AppendLine("    gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;");
            _vertex.AppendLine("    gl_FrontColor = gl_Color;");
            _vertex.AppendLine("    gl_FrontSecondaryColor = gl_SecondaryColor;");
            for (int i = 0; i < _material.NumTexgens; i++)
            {
                /*if (mat.TexGen[i].Src == 1) vert.AppendFormat("    gl_TexCoord[{0}].st = gl_Normal.xy;\n", i);
                else if (mat.TexGen[i].Src != 4 + i) throw new Exception("!texgen " + mat.TexGen[i].Src.ToString());
                else
                vert.AppendFormat("    gl_TexCoord[{0}] = gl_MultiTexCoord{0};\n", i);*/
                // TODO matrices
                _vertex.AppendFormat("    gl_TexCoord[{0}] = {1};\n", i, GenSrc[_material.TexGen[i].Src]);
            }
            _vertex.AppendLine("}");
        }

        private void FragmentStringJoint() 
        {
            _fragment.AppendLine("#version 120");
            _fragment.AppendLine("");

            for (int i = 0; i < 8; i++)
            {
                if (_material.TexStages[i] == 0xFFFF) continue;
                _fragment.AppendLine("uniform sampler2D texture" + i.ToString() + ";");
            }

            _fragment.AppendLine("");
            _fragment.AppendLine("float truncc1(float c)");
            _fragment.AppendLine("{");
            _fragment.AppendLine("    return (c == 0.0) ? 0.0 : ((fract(c) == 0.0) ? 1.0 : fract(c));");
            _fragment.AppendLine("}");
            _fragment.AppendLine("");
            _fragment.AppendLine("vec3 truncc3(vec3 c)");
            _fragment.AppendLine("{");
            _fragment.AppendLine("    return vec3(truncc1(c.r), truncc1(c.g), truncc1(c.b));");
            _fragment.AppendLine("}");
            _fragment.AppendLine("");
            _fragment.AppendLine("void main()");
            _fragment.AppendLine("{");

            for (int i = 0; i < 4; i++)
            {
                int _i = (i == 0) ? 3 : i - 1; // ???
                _fragment.AppendFormat(_forceusa, "    vec4 {0} = vec4({1}, {2}, {3}, {4});\n",
                    outputregs[i],
                    (float)_material.ColorS10[_i].R / 255f, (float)_material.ColorS10[_i].G / 255f,
                    (float)_material.ColorS10[_i].B / 255f, (float)_material.ColorS10[_i].A / 255f);
            }

            for (int i = 0; i < 4; i++)
            {
                _fragment.AppendFormat(_forceusa, "    vec4 k{0} = vec4({1}, {2}, {3}, {4});\n",
                    i,
                    (float)_material.ConstColors[i].R / 255f, (float)_material.ConstColors[i].G / 255f,
                    (float)_material.ConstColors[i].B / 255f, (float)_material.ConstColors[i].A / 255f);
            }

            _fragment.AppendLine("    vec4 texcolor, rascolor, konst;");

            for (int i = 0; i < _material.NumTevStages; i++)
            {
                _fragment.AppendLine("\n    // TEV stage " + i.ToString());

                // TEV inputs
                // for registers prev/0/1/2: use fract() to emulate truncation
                // if they're selected into a, b or c
                string rout, a, b, c, d, operation = "";

                _fragment.AppendLine("    konst.rgb = " + c_konstsel[_material.ConstColorSel[i]] + ";");
                _fragment.AppendLine("    konst.a = " + a_konstsel[_material.ConstAlphaSel[i]] + ";");
                if (_material.TevOrder[i].TexMap != 0xFF && _material.TevOrder[i].TexcoordId != 0xFF)
                    _fragment.AppendFormat("    texcolor = texture2D(texture{0}, gl_TexCoord[{1}].st);\n",
                        _material.TevOrder[i].TexMap, _material.TevOrder[i].TexcoordId);
                _fragment.AppendLine("    rascolor = gl_Color;");
                // TODO: take mat.TevOrder[i].ChanId into account
                // TODO: tex/ras swizzle? (important or not?)
                //mat.TevSwapMode[0].

                if (_material.TevOrder[i].ChanID != 4)
                    throw new Exception("!UNSUPPORTED CHANID " + _material.TevOrder[i].ChanID.ToString());

                rout = outputregs[_material.TevStage[i].ColorRegID] + ".rgb";
                a = c_inputregs[_material.TevStage[i].ColorIn[0]];
                b = c_inputregs[_material.TevStage[i].ColorIn[1]];
                c = c_inputregs[_material.TevStage[i].ColorIn[2]];
                d = c_inputregsD[_material.TevStage[i].ColorIn[3]];

                switch (_material.TevStage[i].ColorOp)
                {
                    case 0:
                        operation = "    {0} = ({4} + mix({1},{2},{3}) + vec3({5},{5},{5})) * vec3({6},{6},{6});";
                        if (_material.TevStage[i].ColorClamp != 0) operation += "\n    {0} = clamp({0}, vec3(0.0,0.0,0.0), vec3(1.0,1.0,1.0));";
                        break;

                    case 1:
                        operation = "    {0} = ({4} - mix({1},{2},{3}) + vec3({5},{5},{5})) * vec3({6},{6},{6});";
                        if (_material.TevStage[i].ColorClamp != 0) operation += "\n    {0} = clamp({0}, vec3(0.0,0.0,0.0), vec3(1.0,1.0,1.0));";
                        break;

                    case 8:
                        operation = "    {0} = {4} + ((({1}).r > ({2}).r) ? {3} : vec(0.0,0.0,0.0));";
                        break;

                    default:
                        operation = "    {0} = vec3(1.0,0.0,1.0);";
                        throw new Exception("!colorop " + _material.TevStage[i].ColorOp.ToString());
                }

                operation = string.Format(operation,
                    rout, a, b, c, d, tevbias[_material.TevStage[i].ColorBias],
                    tevscale[_material.TevStage[i].ColorScale]);
                _fragment.AppendLine(operation);

                rout = outputregs[_material.TevStage[i].AlphaRegID] + ".a";
                a = a_inputregs[_material.TevStage[i].AlphaIn[0]];
                b = a_inputregs[_material.TevStage[i].AlphaIn[1]];
                c = a_inputregs[_material.TevStage[i].AlphaIn[2]];
                d = a_inputregsD[_material.TevStage[i].AlphaIn[3]];

                switch (_material.TevStage[i].AlphaOp)
                {
                    case 0:
                        operation = "    {0} = ({4} + mix({1},{2},{3}) + {5}) * {6};";
                        if (_material.TevStage[i].AlphaClamp != 0) operation += "\n   {0} = clamp({0}, 0.0, 1.0);";
                        break;

                    case 1:
                        operation = "    {0} = ({4} - mix({1},{2},{3}) + {5}) * {6};";
                        if (_material.TevStage[i].AlphaClamp != 0) operation += "\n   {0} = clamp({0}, 0.0, 1.0);";
                        break;

                    default:
                        operation = "    {0} = 1.0;";
                        throw new Exception("!alphaop " + _material.TevStage[i].AlphaOp.ToString());
                }

                operation = string.Format(operation,
                    rout, a, b, c, d, tevbias[_material.TevStage[i].AlphaBias],
                    tevscale[_material.TevStage[i].AlphaScale]);
                _fragment.AppendLine(operation);
            }

            _fragment.AppendLine("");
            _fragment.AppendLine("   gl_FragColor.rgb = truncc3(rprev.rgb);");
            _fragment.AppendLine("   gl_FragColor.a = truncc1(rprev.a);");
            _fragment.AppendLine("");

            _fragment.AppendLine("    // Alpha test");
            if (_material.AlphaComp.MergeFunc == 1 && (_material.AlphaComp.Func0 == 7 || _material.AlphaComp.Func1 == 7))
            {
                // always pass -- do nothing :)
            }
            else if (_material.AlphaComp.MergeFunc == 0 && (_material.AlphaComp.Func0 == 0 || _material.AlphaComp.Func1 == 0))
            {
                // never pass
                // (we did all those color/alpha calculations for uh, nothing ;_; )
                _fragment.AppendLine("    discard;");
            }
            else
            {
                string compare0 = string.Format(_forceusa, alphacompare[_material.AlphaComp.Func0], "gl_FragColor.a", (float)_material.AlphaComp.Ref0 / 255f);
                string compare1 = string.Format(_forceusa, alphacompare[_material.AlphaComp.Func1], "gl_FragColor.a", (float)_material.AlphaComp.Ref1 / 255f);
                string fullcompare = "";

                if (_material.AlphaComp.MergeFunc == 1)
                {
                    if (_material.AlphaComp.Func0 == 0) fullcompare = compare1;
                    else if (_material.AlphaComp.Func1 == 0) fullcompare = compare0;
                }
                else if (_material.AlphaComp.MergeFunc == 0)
                {
                    if (_material.AlphaComp.Func0 == 7) fullcompare = compare1;
                    else if (_material.AlphaComp.Func1 == 7) fullcompare = compare0;
                }

                if (fullcompare == "") fullcompare = string.Format(alphacombine[_material.AlphaComp.MergeFunc], compare0, compare1);

                _fragment.AppendLine("    if (!(" + fullcompare + ")) discard;");
            }

            _fragment.AppendLine("}");
        }

        /// <summary>
        /// This method is created for "Vertex" and "Fragment".<br/>
        /// このメソッドは、"Vertex "と "Fragment "を対象に作成されています。
        /// </summary>
        /// <param name="materialShader"></param>
        /// <param name="shaderType">Only "Vertex" or "Fragment"</param>
        private void GenerateAndCompile_Shader(out int materialShader ,ShaderType shaderType) 
        {
            int ID = GL.CreateShader(shaderType);
            materialShader = ID;

            //Determine shader type and output an error if it is not supported.
            if (shaderType == ShaderType.VertexShader)
                GL.ShaderSource(ID, _vertex.ToString());
            else if (shaderType == ShaderType.FragmentShader)
                GL.ShaderSource(ID, _fragment.ToString());
            else 
                throw new Exception("Not support ShaderType");

            GL.CompileShader(ID);

            GL.GetShader(ID, ShaderParameter.CompileStatus, out _success);
            if (_success == 0)
            {
                string log = GL.GetShaderInfoLog(ID);
                throw new Exception($"!Failed to compile {shaderType}: " + log);
                // TODO: better error reporting/logging?
            }
        }

    }
}
