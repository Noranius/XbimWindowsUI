using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation
{
    public class WpfMaterial : IXbimRenderMaterial
    {
        Material _material;
        string _description;
        public bool IsTransparent;

        protected static ILogger logger { get; private set; }

        // empty constructor
        public WpfMaterial()
        {
        }
        
        public WpfMaterial(XbimColour colour)
        {
            _material = MaterialFromColour(colour);
            _description = "Colour " + colour;
            IsTransparent = colour.IsTransparent;
        }

        public static implicit operator Material(WpfMaterial wpfMaterial)
        {
            return wpfMaterial._material;
        }

        public static void SetLogger(ILogger Logger)
        {
            WpfMaterial.logger = Logger;
        }


        public void CreateMaterial(XbimTexture texture)
        {            
            if (texture.ColourMap.Count > 1)
            {
                _material = new MaterialGroup();
                var descBuilder = new StringBuilder();
                descBuilder.Append("Texture");
                
                var transparent = true;
                foreach (var colour in texture.ColourMap)
                {
                    if (!colour.IsTransparent)
                        transparent = false; //only transparent if everything is transparent
                    descBuilder.AppendFormat(" {0}", colour);
                    ((MaterialGroup)_material).Children.Add(MaterialFromColour(colour));
                }
                _description = descBuilder.ToString();
                IsTransparent = transparent;
            }
            else if(texture.ColourMap.Count == 1)
            {
                var colour = texture.ColourMap[0];
                _material = MaterialFromColour(colour);
                _description = "Texture " + colour;
                IsTransparent = colour.IsTransparent;
            }
            else if (texture.HasImageTexture)
            {
                if (texture.TextureDefinition is IIfcImageTexture imageTexture)
                {
                    _material = WpfMaterial.MaterialFromImage(imageTexture);
                }
            }

            _material.Freeze();
        }

        /// <summary>
        /// Obsolete, please use constructor instead. 17 May 2017
        /// </summary>
        /// <param name="colour"></param>
        [Obsolete]
        public void CreateMaterial(XbimColour colour)
        {
            _material = MaterialFromColour(colour);
            _description = "Colour " + colour;
            IsTransparent = colour.IsTransparent;
        }

        private static Material MaterialFromColour(XbimColour colour)
        {
            var col = Color.FromScRgb(colour.Alpha, colour.Red, colour.Green, colour.Blue);
            Brush brush = new SolidColorBrush(col);

            // build material
            Material mat;
            if (colour.SpecularFactor > 0)
                mat = new SpecularMaterial(brush, colour.SpecularFactor * 100);
            else if (colour.ReflectionFactor > 0)
                mat = new  EmissiveMaterial(brush);
            else
                mat = new DiffuseMaterial(brush);

            // freeze and return
            mat.Freeze();
            return mat;
        }

        /// <summary>
        /// Generate a material from an image (jpg, bmp, ...)
        /// </summary>
        /// <param name="pathTexture">Path to the related image</param>
        /// <returns>A <see cref="DiffuseMaterial">DiffuseMaterial</see> with the given texture</returns>
        private static Material MaterialFromImage(string pathTexture)
        {
            BitmapImage textureImage = new BitmapImage(new Uri(pathTexture));
            return WpfMaterial.MaterialFromImage(textureImage);
        }

        /// <summary>
        /// Generate a material from an BitmapImage
        /// </summary>
        /// <param name="textureImage"><see cref="BitmapImage">Bitmapimage</see> for the material</param>
        /// <returns>A <see cref="DiffuseMaterial">DiffuseMaterial</see> with the given texture</returns>
        private static Material MaterialFromImage (BitmapImage textureImage)
        {
            ImageBrush brush = new ImageBrush(textureImage);
            Material textureMaterial = new DiffuseMaterial(brush);
            textureMaterial.Freeze();
            return textureMaterial;
        }

        public string Description => _description;
        
        public bool IsCreated => _material != null;

        private static Material MaterialFromImage(IIfcImageTexture imageTexture)
        {
            //Check if URI is ok
            if (!Uri.IsWellFormedUriString(imageTexture.URLReference, UriKind.RelativeOrAbsolute))
            {
                WpfMaterial.logger.LogWarning("invalid uri: " + imageTexture.URLReference);
                return WpfMaterial.ErrorMaterial();
            }

            string texturePath;
            Uri myUri = new Uri(imageTexture.URLReference, UriKind.RelativeOrAbsolute);
            if (myUri.IsAbsoluteUri)
            {
                texturePath = myUri.LocalPath;
            }
            else
            {
                //create Path relative to source path
                Uri modelPathUri = imageTexture.Model.SourcePath;
                string modelPathString = modelPathUri.LocalPath;
                string modelDir = Path.GetDirectoryName(modelPathString);
                Uri modelDirUri = new Uri(modelDir + "\\");
                Uri absoluteTexturePathUri = new Uri(modelDirUri, myUri);
                texturePath = absoluteTexturePathUri.LocalPath;
            }

            

            //Check file existence
            if (!File.Exists(texturePath))
            {
                WpfMaterial.logger.LogWarning("The File for the texture could not be found. Object ID: " + imageTexture.EntityLabel + ", Path: " + imageTexture.URLReference);
                return WpfMaterial.ErrorMaterial();
            }

            try
            {
                BitmapImage img = new BitmapImage(new Uri(texturePath));
                ImageBrush brush = new ImageBrush(img);
                Material mat = new DiffuseMaterial(brush);
                return mat;
            }
            catch (Exception ex)
            {
                //Fall back
                WpfMaterial.logger.LogWarning("Could not load image texture: " + ex.ToString());
                return WpfMaterial.ErrorMaterial();
            }
        }

        /// <summary>
        /// If a texture could not been loaded from the file,
        /// a red material is used
        /// </summary>
        /// <returns></returns>
        private static Material ErrorMaterial()
        {
            Color col = Color.FromRgb(255, 0, 0);
            Brush brush = new SolidColorBrush(col);
            Material mat = new DiffuseMaterial(brush);
            mat.Freeze();
            return mat;
        }
    }
}
