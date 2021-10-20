using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;

using Aliencube.XslMapper.FunctionApp.Configurations;
using Aliencube.XslMapper.FunctionApp.Extensions;
using Aliencube.XslMapper.FunctionApp.Models;

using ZXing;
using ZXing.PDF417;
using ZXing.PDF417.Internal;

namespace Aliencube.XslMapper.FunctionApp.Helpers
{
    /// <summary>
    /// This represents the helper entity for XML transformation.
    /// </summary>
    public class XmlTransformHelper : IXmlTransformHelper
    {
        private readonly AppSettings _settings;
        private readonly IBlobStorageHelper _helper;
        private readonly XslCompiledTransform _xslt;
        private readonly XsltArgumentList _arguments;
        private bool _disposed;
        private Stream _stylesheetStream;
        private XmlReader _stylesheetReader;
        private Stream _transformStream;
        private XmlWriter _transformWriter;
        private Stream _transformByteStream;
        private XmlReader _transformByteReader;
        private TextReader _transformStringReader;
        private XmlReader _transformTextReader;

        public XmlTransformHelper(AppSettings settings, IBlobStorageHelper helper)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _xslt = new XslCompiledTransform(enableDebug: true);
            _arguments = new XsltArgumentList();
        }

        /// <inheritdoc />
        public async Task<IXmlTransformHelper> LoadXslAsync(string container, string application, string mapper)
        {
            //! Datos del jSon : mapper
            var blob = await _helper
                .LoadBlobAsync(container, application, mapper)
                .ConfigureAwait(false);
            var bytes = new byte[blob.Properties.Length];
            await blob.DownloadToByteArrayAsync(bytes, 0).ConfigureAwait(false);
            return await LoadXslAsync(bytes).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IXmlTransformHelper> LoadXslAsync(byte[] bytes)
        {
            _stylesheetStream = new MemoryStream(bytes);
            _stylesheetReader = XmlReader.Create(_stylesheetStream);
            await Task.Factory.StartNew(() => _xslt
            .Load(_stylesheetReader, new XsltSettings(enableDocumentFunction: true, enableScript: true), stylesheetResolver: null))
                .ConfigureAwait(false);
            return this;
        }

        /// <inheritdoc />
        public async Task<IXmlTransformHelper> AddArgumentsAsync(List<ExtensionObject> eos)
        {
            //! Datos del jSon : extensionObjects
            foreach (var eo in eos)
            {
                var blob = await _helper
                    .LoadBlobAsync(_settings.Containers.ExtensionObjects, eo.Directory, eo.Name)
                    .ConfigureAwait(false);
                var bytes = new byte[blob.Properties.Length];
                await blob.DownloadToByteArrayAsync(bytes, 0)
                    .ConfigureAwait(false);
                var assembly = Assembly.Load(bytes);
                object instance = assembly.CreateInstance(eo.ClassName);
                _arguments.AddExtensionObject(eo.Namespace, instance);
            }

            return this;
        }

        public async Task<IXmlTransformHelper> AddParam(string inputXml)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode ted = doc.LoadXmlDocument(inputXml);
            await Task.Factory.StartNew(delegate
            {
                BarcodeWriter barcodeWriter = new BarcodeWriter();
                barcodeWriter.Format = BarcodeFormat.PDF_417;
                barcodeWriter.Options = new PDF417EncodingOptions
                {
                    ErrorCorrection = PDF417ErrorCorrectionLevel.L5,
                    Height = 3,
                    Width = 9,
                    Compaction = Compaction.BYTE,
                    Margin = 6
                };
                barcodeWriter.Write(ted.OuterXml).Save(Path.GetTempPath() + "timbre.png");
                using (Image image = Image.FromFile(Path.GetTempPath() + "timbre.png"))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        image.Save(memoryStream, ImageFormat.Png);
                        string text = Convert.ToBase64String(memoryStream.ToArray());
                        _arguments.AddParam("TedTimbre", "", "data:image/png;base64," + text);
                    }
                }

            });
            return this;
        }

        /// <inheritdoc />
        public async Task<byte[]> TransformAsync(XmlReader inputXml)
        {
            await Task.Factory.StartNew(() =>
                       {
                           _transformStream = new MemoryStream();
                           _transformWriter = XmlWriter.Create(_transformStream, _xslt.OutputSettings);
                           _xslt.Transform(inputXml, _arguments, _transformWriter);
                       })
                      .ConfigureAwait(false);
            return (_transformStream as MemoryStream).ToArray().RemoveBom();
        }

        /// <inheritdoc />
        public async Task<byte[]> TransformAsync(byte[] inputXml)
        {
            _transformByteStream = new MemoryStream(inputXml);
            _transformByteReader = XmlReader.Create(_transformByteStream);
            return await TransformAsync(_transformByteReader).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<byte[]> TransformAsync(string inputXml)
        {
            _transformStringReader = new StringReader(inputXml);
            _transformTextReader = XmlReader.Create(_transformStringReader);
            return await TransformAsync(_transformTextReader).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">Value indicating whther to be disposing resources or not.</param>
        protected void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (_disposed)
            {
                return;
            }

            if (_stylesheetStream == null)
            {
                _stylesheetStream.Dispose();
            }

            if (_stylesheetReader == null)
            {
                _stylesheetReader.Dispose();
            }

            if (_transformStream == null)
            {
                _transformStream.Dispose();
            }

            if (_transformWriter == null)
            {
                _transformWriter.Dispose();
            }

            if (_transformByteStream == null)
            {
                _transformByteStream.Dispose();
            }

            if (_transformByteReader == null)
            {
                _transformByteReader.Dispose();
            }

            if (_transformStringReader == null)
            {
                _transformStringReader.Dispose();
            }

            if (_transformTextReader == null)
            {
                _transformTextReader.Dispose();
            }

            _disposed = true;
        }
    }
}