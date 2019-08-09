﻿using Content.Client.GameObjects.Components.Actor;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.Graphics.Overlays;
using Content.Shared.GameObjects;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using System.Collections.Generic;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.Graphics.Overlays;

namespace Content.Client.GameObjects
{
    /// <summary>
    /// A character UI component which shows the current damage state of the mob (living/dead)
    /// </summary>
    public class MobUI : SharedMobComponent, ICharacterUI
    {
        /// <summary>
        /// Holds the godot control for the species window 
        /// </summary>
        private MobWindows _windows;

        /// <summary>
        /// An enum representing the current state being applied to the user
        /// </summary>
        private ScreenEffects _currentEffect = ScreenEffects.None;

        // Required dependencies
#pragma warning disable 649
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

        //Relevant interface implementation for the character UI controller
        public Control Scene => _windows;
        public UIPriority Priority => UIPriority.Mob;

        /// <summary>
        /// Allows calculating if we need to act due to this component being controlled by the current mob
        /// </summary>
        private bool CurrentlyControlled => _playerManager.LocalPlayer.ControlledEntity == Owner;

        /// <summary>
        /// Holds the screen effects that can be applied mapped ot their relevant overlay
        /// </summary>
        private Dictionary<ScreenEffects, Overlay> EffectsDictionary;

        public override void OnRemove()
        {
            base.OnRemove();

            _windows.Dispose();

        }

        public override void OnAdd()
        {
            base.OnAdd();

            _windows = new MobWindows();

            EffectsDictionary = new Dictionary<ScreenEffects, Overlay>()
            {
                { ScreenEffects.CircleMask, new CircleMaskOverlay() },
                { ScreenEffects.GradientCircleMask, new GradientCircleMask() }
            };
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            switch (message)
            {
                case HudStateChange msg:
                    if(CurrentlyControlled)
                    {
                        ChangeHudIcon(msg);
                    }
                    break;

                case PlayerAttachedMsg _:
                    ApplyOverlay();
                    break;

                case PlayerDetachedMsg _:
                    RemoveOverlay();
                    break;
            }
        }

        private void ChangeHudIcon(HudStateChange changemessage)
        {
            _windows.ResetTextures();
            foreach (var sprite in changemessage.StateSprites)
            {
                var window = new MobWindow();
                window.SetIcon(sprite);

                _windows.AddChild(window);
            }
            SetOverlay(changemessage);
        }

        private void SetOverlay(HudStateChange message)
        {
            RemoveOverlay();

            _currentEffect = message.effect;

            ApplyOverlay();
        }

        private void RemoveOverlay()
        {
            if (_currentEffect != ScreenEffects.None)
            {
                var appliedeffect = EffectsDictionary[_currentEffect];
                _overlayManager.RemoveOverlay(appliedeffect.ID);
            }

            _currentEffect = ScreenEffects.None;
        }

        private void ApplyOverlay()
        {
            if (_currentEffect != ScreenEffects.None)
            {
                var overlay = EffectsDictionary[_currentEffect];
                if (_overlayManager.HasOverlay(overlay.ID))
                {
                    return;
                }
                _overlayManager.AddOverlay(overlay);
            }
        }

        private class MobWindow : TextureRect
        {
            public MobWindow()
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
                SizeFlagsVertical = SizeFlags.None;

                //Texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Mob/UI/Human/human0.png");
            }

            public void SetIcon(LimbRender limb)
            {
                if (!IoCManager.Resolve<IResourceCache>().TryGetResource<TextureResource>(new ResourcePath("/Textures/Mob/UI/") / limb.Name, out var newtexture))
                {
                    Logger.Info("The Species Health Sprite {0} Does Not Exist", new ResourcePath("/Textures/Mob/UI/") / limb.Name);
                    return;
                }

                Texture = newtexture;
                if (limb.Color.HasValue == true)
                {
                    Modulate = limb.Color.Value;
                }
            }
        }

        private class MobWindows: MarginContainer
        {
            public MobWindows()
            {
                CustomMinimumSize = (32, 32);
            }

            public void ResetTextures()
            {
                DisposeAllChildren();
            }

            //public void SetIcons(List<LimbRender> limbs)
            //{
            //    for (var i = 0; i < limbs.Count; i++)
            //    {
            //        var window = (MobWindow)GetChild(i);
            //        window.SetIcon(limbs[i]);
            //    }
            //}
        }
    }
}
