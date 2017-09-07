﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using VirtoCommerce.LiquidThemeEngine;
using VirtoCommerce.Storefront.Extensions;
using VirtoCommerce.Storefront.Infrastructure;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.StaticContent;

namespace VirtoCommerce.Storefront.Controllers
{
    public class AssetController : StorefrontControllerBase
    {
        private readonly ILiquidThemeEngine _themeEngine;
        private readonly IStaticContentBlobProvider _staticContentBlobProvider;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IWorkContextAccessor _workContextAccessor;
        public AssetController(IWorkContextAccessor workContextAccessor, IStorefrontUrlBuilder urlBuilder, ILiquidThemeEngine themeEngine, 
                               IStaticContentBlobProvider staticContentBlobProvider, IHostingEnvironment hostingEnvironment)
            : base(workContextAccessor, urlBuilder)
        {
            _themeEngine = themeEngine;
            _staticContentBlobProvider = staticContentBlobProvider;
            _hostingEnvironment = hostingEnvironment;
        }

        /// <summary>
        /// GET: /themes/localization.json
        /// Return localization resources for current theme
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [StorefrontRouteAttribute("themes/localization.json")]
        public ActionResult GetThemeLocalizationJson()
        {
            var retVal = _themeEngine.ReadLocalization();
            return Json(retVal);
        }

        /// <summary>
        /// GET: /themes/assets/{*path}
        /// Handle theme assets requests
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpGet]
        [StorefrontRouteAttribute("themes/assets/{*path}")]
        public ActionResult GetThemeAssets(string path)
        {
            var stream = _themeEngine.GetAssetStream(path);
            return stream != null
                ? File(stream, MimeTypes.GetMimeType(path))
                : HandleStaticFiles(path);
        }

        /// <summary>
        /// GET: /assets/{*path}
        /// Handle all static content assets requests
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpGet]
        [StorefrontRouteAttribute("assets/{*path}")]
        public ActionResult GetStaticContentAssets(string path)
        {
            var blobPath = _staticContentBlobProvider.Search(Path.Combine(WorkContext.CurrentStore.Id, "assets"), path, true).FirstOrDefault();
            if (!string.IsNullOrEmpty(blobPath))
            {
                var stream = _staticContentBlobProvider.OpenRead(blobPath);
                if (stream != null)
                {
                    return File(stream, MimeTypes.GetMimeType(blobPath));
                }
            }

            return StatusCode(404, path);
        }

        /// <summary>
        /// Serve static files. This controller called from SeoRoute when it cannot find any other routes for request.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ActionResult HandleStaticFiles(string path)
        {
            path = _hostingEnvironment.MapPath("~/" + path);
            var mimeType = MimeTypes.GetMimeType(path);
            if (System.IO.File.Exists(path) && mimeType != "application/octet-stream")
            {
                return File(path, MimeTypes.GetMimeType(path));
            }
            return StatusCode(404, path);
        }
    }
}