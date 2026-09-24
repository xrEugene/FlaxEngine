// Copyright (c) Wojciech Figat. All rights reserved.

using System.Reflection;
using FlaxEngine;
using FlaxEngine.GUI;

#pragma warning disable 1591

namespace FlaxEditor
{
    /// <summary>
    /// The editor icons atlas. Cached internally to improve performance.
    /// </summary>
    /// <remarks>
    /// Postfix number informs about the sprite resolution (in pixels).
    /// </remarks>
    [HideInEditor]
    public class EditorIcons
    {
        // 12px
        public SpriteHandle DragBar12;
        public SpriteHandle Search12;
        public SpriteHandle WindowDrag12;
        public SpriteHandle CheckBoxIntermediate12;
        public SpriteHandle ArrowRight12;
        public SpriteHandle Settings12;
        public SpriteHandle Cross12;
        public SpriteHandle CheckBoxTick12;
        public SpriteHandle ArrowDown12;

        // 32px
        public SpriteHandle Scalar32;
        public SpriteHandle Translate32;
        public SpriteHandle Rotate32;
        public SpriteHandle Scale32;
        public SpriteHandle Grid32;
        public SpriteHandle Flax32;
        public SpriteHandle RotateSnap32;
        public SpriteHandle ScaleSnap32;
        public SpriteHandle Globe32;
        public SpriteHandle CamSpeed32;
        public SpriteHandle Link32;
        public SpriteHandle BrokenLink32;
        public SpriteHandle Add32;
        public SpriteHandle Left32;
        public SpriteHandle Right32;
        public SpriteHandle Up32;
        public SpriteHandle Down32;
        public SpriteHandle FolderClosed32;
        public SpriteHandle FolderOpen32;
        public SpriteHandle Folder32;
        public SpriteHandle CameraFill32;
        public SpriteHandle Search32;
        public SpriteHandle Info32;
        public SpriteHandle Warning32;
        public SpriteHandle Error32;
        public SpriteHandle Bone32;
        public SpriteHandle BoneFull32;

        // Visject
        public SpriteHandle VisjectBoxOpen32;
        public SpriteHandle VisjectBoxClosed32;
        public SpriteHandle VisjectArrowOpen32;
        public SpriteHandle VisjectArrowClosed32;

        // 64px
        public SpriteHandle Flax64;
        public SpriteHandle Save64;
        public SpriteHandle Play64;
        public SpriteHandle Stop64;
        public SpriteHandle Pause64;
        public SpriteHandle Skip64;
        public SpriteHandle Info64;
        public SpriteHandle Error64;
        public SpriteHandle Warning64;
        public SpriteHandle AddFile64;
        public SpriteHandle DeleteFile64;
        public SpriteHandle Import64;
        public SpriteHandle Left64;
        public SpriteHandle Right64;
        public SpriteHandle Up64;
        public SpriteHandle Down64;
        public SpriteHandle Undo64;
        public SpriteHandle Redo64;
        public SpriteHandle Translate64;
        public SpriteHandle Rotate64;
        public SpriteHandle Scale64;
        public SpriteHandle Refresh64;
        public SpriteHandle Shift64;
        public SpriteHandle Code64;
        public SpriteHandle Folder64;
        public SpriteHandle CenterView64;
        public SpriteHandle Image64;
        public SpriteHandle Camera64;
        public SpriteHandle Docs64;
        public SpriteHandle Search64;
        public SpriteHandle Bone64;
        public SpriteHandle Link64;
        public SpriteHandle BrokenLink64;
        public SpriteHandle Build64;
        public SpriteHandle Add64;
        public SpriteHandle ShipIt64;
        public SpriteHandle SplineFree64;
        public SpriteHandle SplineLinear64;
        public SpriteHandle SplineAligned64;
        public SpriteHandle SplineSmoothIn64;

        // 96px
        public SpriteHandle Toolbox96;
        public SpriteHandle Paint96;
        public SpriteHandle Foliage96;
        public SpriteHandle Terrain96;

        // 128px
        public SpriteHandle AndroidSettings128;
        public SpriteHandle PlaystationSettings128;
        public SpriteHandle InputSettings128;
        public SpriteHandle PhysicsSettings128;
        public SpriteHandle CSharpScript128;
        public SpriteHandle Folder128;
        public SpriteHandle WindowsIcon128;
        public SpriteHandle LinuxIcon128;
        public SpriteHandle UWPSettings128;
        public SpriteHandle XBOXSettings128;
        public SpriteHandle LayersTagsSettings128;
        public SpriteHandle GraphicsSettings128;
        public SpriteHandle CPPScript128;
        public SpriteHandle Plugin128;
        public SpriteHandle XBoxScarletIcon128;
        public SpriteHandle AssetShadow128;
        public SpriteHandle WindowsSettings128;
        public SpriteHandle TimeSettings128;
        public SpriteHandle GameSettings128;
        public SpriteHandle VisualScript128;
        public SpriteHandle Document128;
        public SpriteHandle XBoxOne128;
        public SpriteHandle UWPStore128;
        public SpriteHandle ColorWheel128;
        public SpriteHandle LinuxSettings128;
        public SpriteHandle NavigationSettings128;
        public SpriteHandle AudioSettings128;
        public SpriteHandle BuildSettings128;
        public SpriteHandle Scene128;
        public SpriteHandle AndroidIcon128;
        public SpriteHandle PS4Icon128;
        public SpriteHandle PS5Icon128;
        public SpriteHandle MacOSIcon128;
        public SpriteHandle IOSIcon128;
        public SpriteHandle FlaxLogo128;
        public SpriteHandle SwitchIcon128;
        public SpriteHandle SwitchSettings128;
        public SpriteHandle LocalizationSettings128;
        public SpriteHandle Json128;
        public SpriteHandle AppleSettings128;
        public SpriteHandle Web128;
        public SpriteHandle WebSettings128;

        /// <summary>
        /// The brush for the <see cref="ArrowRight12"/> icon. Defaults to a sprite brush wrapping <see cref="ArrowRight12"/>; a subclass can replace it with a custom texture-based icon.
        /// </summary>
        public IBrush ArrowRight12Brush;

        /// <summary>
        /// The brush for the <see cref="Right32"/> icon. Defaults to a sprite brush wrapping <see cref="Right32"/>; a subclass can replace it with a custom texture-based icon.
        /// </summary>
        public IBrush Right32Brush;

        /// <summary>
        /// The brush for the <see cref="Right64"/> icon. Defaults to a sprite brush wrapping <see cref="Right64"/>; a subclass can replace it with a custom texture-based icon.
        /// </summary>
        public IBrush Right64Brush;

        /// <summary>
        /// The brush for the <see cref="ArrowDown12"/> icon. Defaults to a sprite brush wrapping <see cref="ArrowDown12"/>; a subclass can replace it with a custom texture-based icon.
        /// </summary>
        public IBrush ArrowDown12Brush;

        /// <summary>
        /// The brush for the <see cref="Down32"/> icon. Defaults to a sprite brush wrapping <see cref="Down32"/>; a subclass can replace it with a custom texture-based icon.
        /// </summary>
        public IBrush Down32Brush;

        /// <summary>
        /// The brush for the <see cref="Down64"/> icon. Defaults to a sprite brush wrapping <see cref="Down64"/>; a subclass can replace it with a custom texture-based icon.
        /// </summary>
        public IBrush Down64Brush;

        /// <summary>
        /// The brush for the <see cref="Left32"/> icon. Defaults to a sprite brush wrapping <see cref="Left32"/>; a subclass can replace it with a custom texture-based icon.
        /// </summary>
        public IBrush Left32Brush;

        /// <summary>
        /// The brush for the <see cref="Left64"/> icon. Defaults to a sprite brush wrapping <see cref="Left64"/>; a subclass can replace it with a custom texture-based icon.
        /// </summary>
        public IBrush Left64Brush;

        /// <summary>
        /// The brush for the <see cref="Up32"/> icon. Defaults to a sprite brush wrapping <see cref="Up32"/>; a subclass can replace it with a custom texture-based icon.
        /// </summary>
        public IBrush Up32Brush;

        /// <summary>
        /// The brush for the <see cref="Up64"/> icon. Defaults to a sprite brush wrapping <see cref="Up64"/>; a subclass can replace it with a custom texture-based icon.
        /// </summary>
        public IBrush Up64Brush;

        /// <summary>
        /// The brush for the <see cref="Cross12"/> icon. Defaults to a sprite brush wrapping <see cref="Cross12"/>; a subclass can replace it with a custom texture-based icon.
        /// </summary>
        public IBrush Cross12Brush;

        internal virtual void LoadIcons()
        {
            // Load & validate
            var iconsAtlas = FlaxEngine.Content.LoadAsyncInternal<SpriteAtlas>(EditorAssets.IconsAtlas);
            if (iconsAtlas is null || iconsAtlas.WaitForLoaded())
            {
                Editor.LogError("Failed to load editor icons atlas.");
                return;
            }

            // Find icons
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.FieldType != typeof(SpriteHandle))
                    continue;

                var sprite = iconsAtlas.FindSprite(field.Name);
                if (!sprite.IsValid)
                    Editor.LogWarning($"Failed to load sprite icon \'{field.Name}\'.");

                field.SetValue(this, sprite);
            }

            ArrowRight12Brush = new SpriteBrush(ArrowRight12);
            Right32Brush = new SpriteBrush(Right32);
            Right64Brush = new SpriteBrush(Right64);
            ArrowDown12Brush = new SpriteBrush(ArrowDown12);
            Down32Brush = new SpriteBrush(Down32);
            Down64Brush = new SpriteBrush(Down64);
            Left32Brush = new SpriteBrush(Left32);
            Left64Brush = new SpriteBrush(Left64);
            Up32Brush = new SpriteBrush(Up32);
            Up64Brush = new SpriteBrush(Up64);
            Cross12Brush = new SpriteBrush(Cross12);
        }
    }
}
