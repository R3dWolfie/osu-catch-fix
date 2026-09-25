// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Catch.Objects.Drawables;
using osu.Game.Rulesets.Catch.UI;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Catch.Skinning.Legacy
{
    public abstract partial class LegacyCatchHitObjectPiece : PoolableDrawable
    {
        protected readonly Bindable<int> IndexInBeatmap = new Bindable<int>();

        private readonly Bindable<Color4> accentColour = new Bindable<Color4>();
        private readonly Bindable<bool> hyperDash = new Bindable<bool>();

        private readonly Sprite colouredSprite;
        private readonly Sprite overlaySprite;
        private readonly Sprite hyperSprite;

        [Resolved]
        protected ISkinSource Skin { get; private set; } = null!;

        [Resolved]
        protected IHasCatchObjectState ObjectState { get; private set; } = null!;

        protected LegacyCatchHitObjectPiece()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                colouredSprite = new Sprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                overlaySprite = new Sprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                hyperSprite = new Sprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Blending = BlendingParameters.Additive,
                    Depth = 1,
                    Alpha = 0,
                    Scale = new Vector2(1.2f),
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            accentColour.BindTo(ObjectState.AccentColour);
            hyperDash.BindTo(ObjectState.HyperDash);
            IndexInBeatmap.BindTo(ObjectState.IndexInBeatmap);

            hyperSprite.Colour = Skin.GetConfig<CatchSkinColour, Color4>(CatchSkinColour.HyperDashFruit)?.Value ??
                                 Skin.GetConfig<CatchSkinColour, Color4>(CatchSkinColour.HyperDash)?.Value ??
                                 Catcher.DEFAULT_HYPER_DASH_COLOUR;

            accentColour.BindValueChanged(colour =>
            {
                colouredSprite.Colour = LegacyColourCompatibility.DisallowZeroAlpha(colour.NewValue);
            }, true);

            hyperDash.BindValueChanged(hyper =>
            {
                hyperSprite.Alpha = hyper.NewValue ? 0.7f : 0;
            }, true);
        }

        protected override void Update()
        {
            base.Update();

            if (!hyperDash.Value)
                return;

            // stable pulses the hyper glow on a 500ms loop starting when the fruit appears: it grows slightly and fades, then snaps back.
            double sinceAppear = Time.Current - (ObjectState.HitObject.StartTime - ObjectState.HitObject.TimePreempt);
            float progress = (float)(((sinceAppear % hyper_pulse_duration) + hyper_pulse_duration) % hyper_pulse_duration / hyper_pulse_duration);

            hyperSprite.Scale = new Vector2(1.215f + 0.075f * progress);
            hyperSprite.Alpha = 0.96f - 0.38f * progress;
        }

        private const double hyper_pulse_duration = 500;

        protected void SetTexture(Texture? texture, Texture? overlayTexture)
        {
            // Sizes are reset due to an arguable osu!framework bug where Sprite retains the size of the first set texture.

            if (colouredSprite.Texture != texture)
            {
                colouredSprite.Size = Vector2.Zero;
                colouredSprite.Texture = texture;
            }

            if (overlaySprite.Texture != overlayTexture)
            {
                overlaySprite.Size = Vector2.Zero;
                overlaySprite.Texture = overlayTexture;
            }

            if (hyperSprite.Texture != texture)
            {
                hyperSprite.Size = Vector2.Zero;
                hyperSprite.Texture = texture;
            }
        }
    }
}
