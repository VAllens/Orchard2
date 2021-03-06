﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Orchard.Environment.Extensions.Features;
using Orchard.Environment.Extensions.Manifests;
using System;
using System.Linq;

namespace Orchard.Environment.Extensions
{
    public class ExtensionProvider : IExtensionProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly IManifestBuilder _manifestBuilder;
        private readonly IFeatureManager _featureManager;

        /// <summary>
        /// Initializes a new instance of a ExtensionProvider at the given root directory.
        /// </summary>
        /// <param name="hostingEnvironment">hostingEnvironment containing the fileproviders.</param>
        /// <param name="manifestBuilder">The manifest provider.</param>
        /// <param name="featureManager">The feature manager.</param>
        public ExtensionProvider(
            IHostingEnvironment hostingEnvironment,
            IManifestBuilder manifestBuilder,
            IFeatureManager featureManager)
        {
            _fileProvider = hostingEnvironment.ContentRootFileProvider;
            _manifestBuilder = manifestBuilder;
            _featureManager = featureManager;
        }

        /// <summary>
        /// Locate an extension at the given path by directly mapping path segments to physical directories.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <returns>The extension information. null returned if extension does not exist</returns>
        public IExtensionInfo GetExtensionInfo(string subPath)
        {
            var manifest = _manifestBuilder.GetManifest(subPath);
            
            if (!manifest.Exists)
            {
                return null;
            }

            var path = System.IO.Path.GetDirectoryName(subPath);
            var name = System.IO.Path.GetFileName(subPath);

            var extension = _fileProvider
                .GetDirectoryContents(path)
                .First(content => content.Name == name);

            return new ExtensionInfo(extension, subPath, manifest, (ei) => {
                return _featureManager.GetFeatures(ei, manifest);
            });
        }
    }
}
