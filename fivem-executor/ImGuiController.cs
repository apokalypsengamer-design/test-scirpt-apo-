using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace FiveM.Executor
{
    public class ImGuiController : IDisposable
    {
        private SharpDX.Direct3D11.Device _device;
        private DeviceContext _deviceContext;
        private Factory _factory;
        private IntPtr _fontTextureResourceView;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private InputLayout _inputLayout;
        private SharpDX.Direct3D11.Buffer _vertexBuffer;
        private SharpDX.Direct3D11.Buffer _indexBuffer;
        private SharpDX.Direct3D11.Buffer _constantBuffer;
        private BlendState _blendState;
        private RasterizerState _rasterizerState;
        private DepthStencilState _depthStencilState;
        private SamplerState _samplerState;
        private int _vertexBufferSize = 5000;
        private int _indexBufferSize = 10000;

        public ImGuiController(SharpDX.Direct3D11.Device device, DeviceContext deviceContext)
        {
            _device = device;
            _deviceContext = deviceContext;
            _factory = new Factory();

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            ImGuiIOPtr io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.DisplaySize = new Vector2(1920, 1080);

            ImGui.StyleColorsDark();

            CreateDeviceObjects();
            CreateFontsTexture();
        }

        private void CreateDeviceObjects()
        {
            string vertexShaderCode = @"
                cbuffer vertexBuffer : register(b0) 
                {
                    float4x4 ProjectionMatrix; 
                };
                
                struct VS_INPUT
                {
                    float2 pos : POSITION;
                    float4 col : COLOR0;
                    float2 uv  : TEXCOORD0;
                };
                
                struct PS_INPUT
                {
                    float4 pos : SV_POSITION;
                    float4 col : COLOR0;
                    float2 uv  : TEXCOORD0;
                };
                
                PS_INPUT main(VS_INPUT input)
                {
                    PS_INPUT output;
                    output.pos = mul(ProjectionMatrix, float4(input.pos.xy, 0.f, 1.f));
                    output.col = input.col;
                    output.uv  = input.uv;
                    return output;
                }";

            var vertexShaderByteCode = SharpDX.D3DCompiler.ShaderBytecode.Compile(
                vertexShaderCode, "main", "vs_5_0");
            _vertexShader = new VertexShader(_device, vertexShaderByteCode);

            var inputElements = new[]
            {
                new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 8, 0),
                new InputElement("COLOR", 0, Format.R8G8B8A8_UNorm, 16, 0)
            };
            _inputLayout = new InputLayout(_device, vertexShaderByteCode, inputElements);

            string pixelShaderCode = @"
                struct PS_INPUT
                {
                    float4 pos : SV_POSITION;
                    float4 col : COLOR0;
                    float2 uv  : TEXCOORD0;
                };
                
                sampler sampler0;
                Texture2D texture0;
                
                float4 main(PS_INPUT input) : SV_Target
                {
                    float4 out_col = input.col * texture0.Sample(sampler0, input.uv); 
                    return out_col; 
                }";

            var pixelShaderByteCode = SharpDX.D3DCompiler.ShaderBytecode.Compile(
                pixelShaderCode, "main", "ps_5_0");
            _pixelShader = new PixelShader(_device, pixelShaderByteCode);

            var constantBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 64,
                StructureByteStride = 0
            };
            _constantBuffer = new SharpDX.Direct3D11.Buffer(_device, constantBufferDesc);

            var blendStateDesc = new BlendStateDescription
            {
                AlphaToCoverageEnable = false
            };
            blendStateDesc.RenderTarget[0].IsBlendEnabled = true;
            blendStateDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            blendStateDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            blendStateDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blendStateDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            blendStateDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
            blendStateDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            blendStateDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            _blendState = new BlendState(_device, blendStateDesc);

            var rasterizerStateDesc = new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                IsScissorEnabled = true,
                IsDepthClipEnabled = true
            };
            _rasterizerState = new RasterizerState(_device, rasterizerStateDesc);

            var depthStencilStateDesc = new DepthStencilStateDescription
            {
                IsDepthEnabled = false,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Always,
                IsStencilEnabled = false,
                StencilReadMask = 0,
                StencilWriteMask = 0
            };
            _depthStencilState = new DepthStencilState(_device, depthStencilStateDesc);

            var samplerStateDesc = new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLodBias = 0f,
                ComparisonFunction = Comparison.Always,
                MinimumLod = 0f,
                MaximumLod = 0f
            };
            _samplerState = new SamplerState(_device, samplerStateDesc);

            CreateVertexBuffer();
            CreateIndexBuffer();
        }

        private void CreateVertexBuffer()
        {
            var desc = new BufferDescription
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = _vertexBufferSize * Unsafe.SizeOf<ImDrawVert>(),
                StructureByteStride = 0
            };
            _vertexBuffer = new SharpDX.Direct3D11.Buffer(_device, desc);
        }

        private void CreateIndexBuffer()
        {
            var desc = new BufferDescription
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.IndexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = _indexBufferSize * sizeof(ushort),
                StructureByteStride = 0
            };
            _indexBuffer = new SharpDX.Direct3D11.Buffer(_device, desc);
        }

        private void CreateFontsTexture()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

            var textureDesc = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R8G8B8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            var dataBox = new DataBox(pixels, width * bytesPerPixel, 0);
            using (var texture = new Texture2D(_device, textureDesc, new[] { dataBox }))
            {
                var shaderResourceViewDesc = new ShaderResourceViewDescription
                {
                    Format = textureDesc.Format,
                    Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                    Texture2D = { MipLevels = textureDesc.MipLevels }
                };
                var textureView = new ShaderResourceView(_device, texture, shaderResourceViewDesc);
                _fontTextureResourceView = textureView.NativePointer;
            }

            io.Fonts.SetTexID(_fontTextureResourceView);
            io.Fonts.ClearTexData();
        }

        public void NewFrame(float deltaTime, int width, int height)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new Vector2(width, height);
            io.DeltaTime = deltaTime > 0 ? deltaTime : 1f / 60f;

            ImGui.NewFrame();
        }

        public void Render()
        {
            ImGui.Render();
            RenderDrawData(ImGui.GetDrawData());
        }

        private unsafe void RenderDrawData(ImDrawDataPtr drawData)
        {
            if (drawData.DisplaySize.X <= 0.0f || drawData.DisplaySize.Y <= 0.0f)
                return;

            if (_vertexBuffer == null || _vertexBufferSize < drawData.TotalVtxCount)
            {
                _vertexBuffer?.Dispose();
                _vertexBufferSize = drawData.TotalVtxCount + 5000;
                CreateVertexBuffer();
            }

            if (_indexBuffer == null || _indexBufferSize < drawData.TotalIdxCount)
            {
                _indexBuffer?.Dispose();
                _indexBufferSize = drawData.TotalIdxCount + 10000;
                CreateIndexBuffer();
            }

            DataStream vtxResource = null;
            DataStream idxResource = null;

            try
            {
                var vtxBox = _deviceContext.MapSubresource(_vertexBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out vtxResource);
                var idxBox = _deviceContext.MapSubresource(_indexBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out idxResource);

                int vtxOffset = 0;
                int idxOffset = 0;

                for (int n = 0; n < drawData.CmdListsCount; n++)
                {
                    ImDrawListPtr cmdList = drawData.CmdLists[n];
                    
                    var vtxSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
                    vtxResource.WriteRange(cmdList.VtxBuffer.Data, vtxSize);
                    
                    var idxSize = cmdList.IdxBuffer.Size * sizeof(ushort);
                    idxResource.WriteRange(cmdList.IdxBuffer.Data, idxSize);

                    vtxOffset += cmdList.VtxBuffer.Size;
                    idxOffset += cmdList.IdxBuffer.Size;
                }

                _deviceContext.UnmapSubresource(_vertexBuffer, 0);
                _deviceContext.UnmapSubresource(_indexBuffer, 0);
            }
            finally
            {
                vtxResource?.Dispose();
                idxResource?.Dispose();
            }

            var L = drawData.DisplayPos.X;
            var R = drawData.DisplayPos.X + drawData.DisplaySize.X;
            var T = drawData.DisplayPos.Y;
            var B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;
            float[] mvp = {
                2.0f/(R-L),     0.0f,           0.0f, 0.0f,
                0.0f,           2.0f/(T-B),     0.0f, 0.0f,
                0.0f,           0.0f,           0.5f, 0.0f,
                (R+L)/(L-R),    (T+B)/(B-T),    0.5f, 1.0f,
            };

            DataStream constantResource = null;
            try
            {
                _deviceContext.MapSubresource(_constantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out constantResource);
                constantResource.WriteRange(mvp);
                _deviceContext.UnmapSubresource(_constantBuffer, 0);
            }
            finally
            {
                constantResource?.Dispose();
            }

            var viewport = new Viewport(0, 0, drawData.DisplaySize.X, drawData.DisplaySize.Y, 0.0f, 1.0f);
            _deviceContext.Rasterizer.SetViewport(viewport);

            _deviceContext.InputAssembler.InputLayout = _inputLayout;
            _deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, Unsafe.SizeOf<ImDrawVert>(), 0));
            _deviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);
            _deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            _deviceContext.VertexShader.Set(_vertexShader);
            _deviceContext.VertexShader.SetConstantBuffer(0, _constantBuffer);
            _deviceContext.PixelShader.Set(_pixelShader);
            _deviceContext.PixelShader.SetSampler(0, _samplerState);

            _deviceContext.OutputMerger.SetBlendState(_blendState);
            _deviceContext.OutputMerger.SetDepthStencilState(_depthStencilState);
            _deviceContext.Rasterizer.State = _rasterizerState;

            int vtxOffset2 = 0;
            int idxOffset2 = 0;
            Vector2 clipOff = drawData.DisplayPos;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmdList = drawData.CmdLists[n];
                for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
                {
                    ImDrawCmdPtr cmd = cmdList.CmdBuffer[i];

                    if (cmd.UserCallback != IntPtr.Zero)
                    {
                    }
                    else
                    {
                        var clipRect = new SharpDX.Rectangle(
                            (int)(cmd.ClipRect.X - clipOff.X),
                            (int)(cmd.ClipRect.Y - clipOff.Y),
                            (int)(cmd.ClipRect.Z - clipOff.X),
                            (int)(cmd.ClipRect.W - clipOff.Y));

                        _deviceContext.Rasterizer.SetScissorRectangle(clipRect.Left, clipRect.Top, clipRect.Right, clipRect.Bottom);

                        var textureId = cmd.TextureId;
                        var srv = new ShaderResourceView(_device, textureId);
                        _deviceContext.PixelShader.SetShaderResource(0, srv);

                        _deviceContext.DrawIndexed((int)cmd.ElemCount, (int)(cmd.IdxOffset + idxOffset2), (int)(cmd.VtxOffset + vtxOffset2));
                    }
                }
                idxOffset2 += cmdList.IdxBuffer.Size;
                vtxOffset2 += cmdList.VtxBuffer.Size;
            }
        }

        public void UpdateInput(System.Windows.Forms.MouseEventArgs e = null, System.Windows.Forms.KeyEventArgs keyEvent = null, char keyChar = '\0')
        {
            ImGuiIOPtr io = ImGui.GetIO();

            if (e != null)
            {
                io.MousePos = new Vector2(e.X, e.Y);

                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                    io.MouseDown[0] = true;
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                    io.MouseDown[1] = true;
                if (e.Button == System.Windows.Forms.MouseButtons.Middle)
                    io.MouseDown[2] = true;
            }

            if (keyEvent != null)
            {
                if (keyEvent.KeyCode >= 0 && (int)keyEvent.KeyCode < 512)
                {
                    io.KeysDown[(int)keyEvent.KeyCode] = keyEvent is System.Windows.Forms.KeyEventArgs;
                }

                io.KeyCtrl = keyEvent.Control;
                io.KeyShift = keyEvent.Shift;
                io.KeyAlt = keyEvent.Alt;
            }

            if (keyChar != '\0')
            {
                io.AddInputCharacter(keyChar);
            }
        }

        public void Dispose()
        {
            _vertexShader?.Dispose();
            _pixelShader?.Dispose();
            _inputLayout?.Dispose();
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _constantBuffer?.Dispose();
            _blendState?.Dispose();
            _rasterizerState?.Dispose();
            _depthStencilState?.Dispose();
            _samplerState?.Dispose();
            _factory?.Dispose();
        }
    }
}
