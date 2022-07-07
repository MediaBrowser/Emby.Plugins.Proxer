using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Drawing;
using System.IO;

namespace Emby.Plugins.Proxer
{
    public class Plugin : BasePlugin, IHasWebPages, IHasThumbImage
    {
        public override string Name
        {
            get { return "Proxer"; }
        }

        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return Array.Empty<PluginPageInfo>();
        }

        private Guid _id = new Guid("C71894B5-FCBF-4322-A8F6-90E633B72AA5");

        public override Guid Id
        {
            get { return _id; }
        }

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
        }
    }
}