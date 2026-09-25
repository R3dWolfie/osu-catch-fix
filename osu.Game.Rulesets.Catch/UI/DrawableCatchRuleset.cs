// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Input;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osuTK;

namespace osu.Game.Rulesets.Catch.UI
{
    public partial class DrawableCatchRuleset : DrawableScrollingRuleset<CatchHitObject>
    {
        protected override bool UserScrollSpeedAdjustment => false;

        public DrawableCatchRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod>? mods = null)
            : base(ruleset, beatmap, mods)
        {
            Direction.Value = ScrollingDirection.Down;
            TimeRange.Value = GetTimeRange(beatmap.Difficulty.ApproachRate);
            VisualisationMethod = ScrollVisualisationMethod.Constant;
        }

        [Resolved]
        private GameHost host { get; set; } = null!;

        private double? previousDrawHz;
        private double? previousUpdateHz;

        [BackgroundDependencyLoader]
        private void load()
        {
            // With relax mod, input maps directly to x position and left/right buttons are not used.
            if (!Mods.Any(m => m is ModRelax))
                KeyBindingInputManager.Add(new CatchTouchInputMapper());
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // catch is played by watching the catcher, so run update and draw at full rate during gameplay to keep input to screen delay low.
            // the previous rates are restored on dispose.
            previousDrawHz = host.MaximumDrawHz;
            previousUpdateHz = host.MaximumUpdateHz;

            host.MaximumUpdateHz = Math.Max(host.MaximumUpdateHz, 1000);
            host.MaximumDrawHz = Math.Max(host.MaximumDrawHz, host.MaximumUpdateHz);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (previousDrawHz != null)
                host.MaximumDrawHz = previousDrawHz.Value;
            if (previousUpdateHz != null)
                host.MaximumUpdateHz = previousUpdateHz.Value;

            base.Dispose(isDisposing);
        }

        protected double GetTimeRange(float approachRate) => IBeatmapDifficultyInfo.DifficultyRange(approachRate, 1800, 1200, 450);

        protected override ReplayInputHandler CreateReplayInputHandler(Replay replay) => new CatchFramedReplayInputHandler(replay);

        protected override ReplayRecorder CreateReplayRecorder(Score score) => new CatchReplayRecorder(score, (CatchPlayfield)Playfield);

        protected override Playfield CreatePlayfield() => new CatchPlayfield(Beatmap.Difficulty);

        public override PlayfieldAdjustmentContainer CreatePlayfieldAdjustmentContainer() => new CatchPlayfieldAdjustmentContainer();

        protected override PassThroughInputManager CreateInputManager() => new CatchInputManager(Ruleset.RulesetInfo);

        public override DrawableHitObject<CatchHitObject>? CreateDrawableRepresentation(CatchHitObject h) => null;

        protected override ResumeOverlay CreateResumeOverlay() => new DelayedResumeOverlay { Scale = new Vector2(0.65f) };
    }
}
