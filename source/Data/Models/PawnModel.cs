﻿using System;
using System.Linq;
using System.Text;
using RimHUD.Data.Configuration;
using RimHUD.Data.Extensions;
using RimHUD.Data.Integration;
using RimHUD.Interface;
using RimHUD.Interface.HUD;
using RimHUD.Patch;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace RimHUD.Data.Models
{
  internal class PawnModel
  {
    public static PawnModel Selected => GetSelected();

    public Pawn Base { get; }

    public HudTarget Target => GetTargetType();

    public string Name => Base.GetName();
    public TextModel GenderRaceAndAge => GetGenderRaceAndAge();
    public TextModel IdeoligionGenderRaceAndAge => GetIdeoligionGenderRaceAndAge();

    public bool IsPlayerFaction => Base.Faction?.IsPlayer ?? false;
    public bool IsPlayerManaged => (Base.Faction?.IsPlayer ?? false) || (Base.HostFaction?.IsPlayer ?? false);

    private Color? _factionRelationColor;
    public Color FactionRelationColor => _factionRelationColor ?? (_factionRelationColor = GetFactionRelationColor()).Value;
    public TextModel RelationKindFaction => GetRelationKindAndFaction();

    public HealthModel Health { get; }
    public MindModel Mind { get; }

    public bool IsHumanlike => Base.RaceProps.Humanlike;
    public bool IsAnimal => Base.RaceProps.Animal;
    public TextModel Master => GetMaster();

    public NeedModel Rest => GetNeedModel(NeedDefOf.Rest);
    public NeedModel Food => GetNeedModel(NeedDefOf.Food);
    public NeedModel Recreation => GetNeedModel(NeedDefOf.Joy);
    public NeedModel Mood => GetNeedModel(Access.NeedDefOfMood);
    public float MoodThresholdMinor => Base.mindState?.mentalBreaker?.BreakThresholdMinor ?? -1f;
    public float MoodThresholdMajor => Base.mindState?.mentalBreaker?.BreakThresholdMajor ?? -1f;
    public float MoodThresholdExtreme => Base.mindState?.mentalBreaker?.BreakThresholdExtreme ?? -1f;

    public SkillModel Shooting => GetSkillModel(SkillDefOf.Shooting);
    public SkillModel Melee => GetSkillModel(SkillDefOf.Melee);
    public SkillModel Construction => GetSkillModel(SkillDefOf.Construction);
    public SkillModel Mining => GetSkillModel(SkillDefOf.Mining);
    public SkillModel Cooking => GetSkillModel(SkillDefOf.Cooking);
    public SkillModel Plants => GetSkillModel(SkillDefOf.Plants);
    public SkillModel Animals => GetSkillModel(SkillDefOf.Animals);
    public SkillModel Crafting => GetSkillModel(SkillDefOf.Crafting);
    public SkillModel Artistic => GetSkillModel(SkillDefOf.Artistic);
    public SkillModel Medicine => GetSkillModel(SkillDefOf.Medicine);
    public SkillModel Social => GetSkillModel(SkillDefOf.Social);
    public SkillModel Intellectual => GetSkillModel(SkillDefOf.Intellectual);

    public TrainingModel Tameness => GetTrainingModel(TrainableDefOf.Tameness);
    public TrainingModel Obedience => GetTrainingModel(TrainableDefOf.Obedience);
    public TrainingModel Release => GetTrainingModel(TrainableDefOf.Release);
    public TrainingModel Rescue => GetTrainingModel(Access.TrainableDefOfRescue);
    public TrainingModel Haul => GetTrainingModel(Access.TrainableDefOfHaul);

    public string Activity => GetActivity();
    public Func<string> ActivityTooltip { get; }
    public string Queued => GetQueued();

    public string Equipped => GetEquipped();
    public string Carrying => GetCarrying();

    public string CompInfo => GetCompInfo();

    public Func<string> BioTooltip { get; }

    public ISelectorModel OutfitSelector => new OutfitModel(this);
    public ISelectorModel FoodSelector => Mod_PawnRules.Instance.IsActive ? RulesSelector : new FoodModel(this);
    public ISelectorModel RulesSelector => Mod_PawnRules.Instance.IsActive ? new RulesModel(this) : null;
    public ISelectorModel TimetableSelector => new TimetableModel(this);
    public ISelectorModel AreaSelector => new AreaModel(this);

    private PawnModel(Pawn pawn)
    {
      Base = pawn;
      Health = new HealthModel(this);
      Mind = new MindModel(this);
      ActivityTooltip = GetActivityTooltip();
      BioTooltip = GetBioTooltip();
    }

    private static PawnModel GetSelected()
    {
      var selected = State.SelectedPawn;
      return selected == null ? null : new PawnModel(selected);
    }

    private HudTarget GetTargetType()
    {
      if (IsPlayerManaged) { return Base.RaceProps.Humanlike ? HudTarget.PlayerHumanlike : HudTarget.PlayerCreature; }
      return Base.RaceProps.Humanlike ? HudTarget.OtherHumanlike : HudTarget.OtherCreature;
    }

    private NeedModel GetNeedModel(NeedDef def) => new NeedModel(this, def);
    private SkillModel GetSkillModel(SkillDef def) => new SkillModel(this, def);
    private TrainingModel GetTrainingModel(TrainableDef def) => new TrainingModel(this, def);

    private string GetGender() => Base.gender == Gender.None ? null : Base.GetGenderLabel();

    private string GetRace() => Base.kindDef?.race?.label;
    private string GetRaceIfNotHuman() => Base.RaceProps?.Humanlike ?? true ? null : Base.kindDef?.race?.label;

    private string GetGenderRaceAndAgeText(bool hideRaceIfHuman)
    {
      var gender = GetGender();
      var genderKind = Lang.AdjectiveNoun(gender, hideRaceIfHuman ? GetRaceIfNotHuman() : GetRace());

      if (Base.ageTracker == null) { return genderKind.Trim(); }

      Base.ageTracker.AgeBiologicalTicks.TicksToPeriod(out var years, out var quadrums, out var days, out _);
      var ageDays = (quadrums * GenDate.DaysPerQuadrum) + days;

      var age = years.ToString().Bold();
      if (ageDays == 0 || ageDays == GenDate.DaysPerYear) { age = Lang.CombineWords(age, Lang.Get("Model.Age.Birthday")); }
      else if (ageDays == 1) { age = Lang.CombineWords(age, Lang.Get("Model.Age.Day")); }
      else { age = Lang.CombineWords(age, Lang.Get("Model.Age.Days", ageDays)); }

      return Lang.Get("Model.GenderRaceAndAge", genderKind, age).Trim();
    }

    private TextModel GetGenderRaceAndAge() => TextModel.Create(GetGenderRaceAndAgeText(false).CapitalizeFirst(), GetBioTooltip(), FactionRelationColor, InspectPanePlus.ToggleBioTab);

    private TextModel GetIdeoligionGenderRaceAndAge()
    {
      var genderRaceAge = GetGenderRaceAndAgeText(true);
      return TextModel.Create(!ModLister.IdeologyInstalled || Base.Ideo == null ? genderRaceAge : Lang.Get("Model.IdeoligionGenderRaceAndAge", Base.Ideo.memberName, genderRaceAge), GetBioTooltip(), FactionRelationColor, InspectPanePlus.ToggleBioTab);
    }

    private string GetKind()
    {
      if (IsHumanlike) { return Base.Faction == Faction.OfPlayer ? Base.story?.Title ?? Base.KindLabel : Base.TraderKind?.label ?? Base.KindLabel; }

      if (Base.Faction == null)
      {
        if (Base.RaceProps.petness > 0.5f) { return Lang.Get("Model.Creature.Stray"); }
        if (Base.RaceProps.predator) { return Lang.Get("Model.Creature.Predator"); }
        if (Base.kindDef.race.tradeTags?.Contains("AnimalInsect") ?? false) { return Lang.Get("Model.Creature.Insect"); }
        return Lang.Get("Model.Creature.Wild");
      }

      if (Base.RaceProps.petness > 0.5f) { return Lang.Get("Model.Creature.Pet"); }
      if (Base.RaceProps.petness > 0f) { return Lang.Get("Model.Creature.ExoticPet"); }
      if (Base.RaceProps.predator) { return Lang.Get("Model.Creature.Hunt"); }
      if (Base.RaceProps.packAnimal) { return Lang.Get("Model.Creature.Pack"); }
      if (Base.kindDef.race.tradeTags?.Contains("AnimalFarm") ?? false) { return Lang.Get("Model.Creature.Farm"); }
      if (Base.RaceProps.herdAnimal) { return Lang.Get("Model.Creature.Herd"); }
      if (Base.kindDef.race.tradeTags?.Contains("AnimalInsect") ?? false) { return Lang.Get("Model.Creature.Insect"); }
      if (Base.RaceProps.Animal) { return Lang.Get("Model.Creature.Tame"); }

      return Lang.Get("Model.Creature.Unit");
    }

    private Color GetFactionRelationColor()
    {
      if (Base.Faction == null) { return IsHumanlike ? Theme.FactionIndependentColor.Value : Theme.FactionWildColor.Value; }
      if (Base.Faction.IsPlayer) { return Theme.FactionOwnColor.Value; }

      if (Base.Faction.PlayerRelationKind == FactionRelationKind.Hostile) { return Theme.FactionHostileColor.Value; }
      return Base.Faction.PlayerRelationKind == FactionRelationKind.Ally ? Theme.FactionAlliedColor.Value : Theme.FactionIndependentColor.Value;
    }

    private string GetFactionRelation()
    {
      if (Base.Faction == null) { return IsHumanlike ? Lang.Get("Model.Faction.Independent") : Base.kindDef == PawnKindDefOf.WildMan ? null : Lang.Get("Model.Faction.Wild"); }
      if (Base.Faction.IsPlayer) { return null; }

      var relation = Base.Faction.PlayerRelationKind;
      if (relation == FactionRelationKind.Hostile) { return Base.RaceProps.IsMechanoid ? Lang.Get("Model.Faction.Hostile") : Lang.Get("Model.Faction.Enemy"); }
      return relation == FactionRelationKind.Ally ? Lang.Get("Model.Faction.Allied") : null;
    }

    private TextModel GetRelationKindAndFaction()
    {
      var faction = Base.Faction == null || !Base.Faction.HasName ? null : Lang.Get("Model.OfFaction", Base.Faction.Name);
      var relationKind = Lang.AdjectiveNoun(GetFactionRelation(), GetKind());

      return TextModel.Create(Lang.Get("Model.RelationKindAndFaction", relationKind, faction).Trim().CapitalizeFirst(), GetBioTooltip(), FactionRelationColor, InspectPanePlus.ToggleBioTab);
    }

    private string GetActivity()
    {
      try
      {
        var lord = Base.GetLord()?.LordJob?.GetReport(Base)?.CapitalizeFirst();
        var jobText = Base.jobs?.curDriver?.GetReport()?.TrimEnd('.').CapitalizeFirst();
        var target = Base.jobs?.curJob?.def == JobDefOf.AttackStatic || Base.jobs?.curJob?.def == JobDefOf.Wait_Combat ? Base.TargetCurrentlyAimingAt.Thing?.LabelCap : null;
        var activity = target == null ? lord.NullOrEmpty() ? jobText : $"{lord} ({jobText})" : Lang.Get("Model.Info.Attacking", target);

        return activity == null ? null : Lang.Get("Model.Info.Activity", activity.Bold());
      }
      catch (Exception exception)
      {
        Mod.HandleWarning(exception);
        return null;
      }
    }

    private Func<string> GetActivityTooltip() => () =>
    {
      try
      {
        var work = Base.CurJob?.workGiverDef?.Worker?.def?.workType?.gerundLabel.CapitalizeFirst();
        if (work == null) { return null; }

        var builder = new StringBuilder();
        builder.AppendLine(Lang.Get("Model.Info.Activity.WorkType", work));
        builder.AppendLine(Lang.Get("Model.Info.Activity.RelevantSkills", Base.CurJob?.workGiverDef?.Worker?.def?.workType?.relevantSkills?.Select(p => p.LabelCap.ToString()).ToCommaList()));

        return builder.ToStringTrimmed().Size(Theme.RegularTextStyle.ActualSize);
      }
      catch (Exception exception)
      {
        Mod.HandleWarning(exception);
        return null;
      }
    };

    private string GetQueued()
    {
      try
      {
        if (Base.jobs?.curJob == null || Base.jobs.jobQueue.Count == 0) { return null; }

        var queued = Base.jobs.jobQueue[0].job.GetReport(Base)?.TrimEnd('.').CapitalizeFirst().Bold();
        var remaining = Base.jobs.jobQueue.Count - 1;
        if (remaining > 0) { queued += $" (+{remaining})"; }

        return queued == null ? null : Lang.Get("Model.Info.Queued", queued);
      }
      catch (Exception exception)
      {
        Mod.HandleWarning(exception);
        return null;
      }
    }

    private string GetCarrying()
    {
      var carried = Base.carryTracker?.CarriedThing?.LabelCap;
      return carried == null ? null : Lang.Get("Model.Info.Carrying", carried.Bold());
    }

    private string GetEquipped()
    {
      var equipped = Base.equipment?.Primary?.LabelCap;

      return RestraintsUtility.ShouldShowRestraintsInfo(Base) ? (string) "InRestraints".Translate() : equipped == null ? null : Lang.Get("Model.Info.Equipped", equipped.Bold());
    }

    private string GetCompInfo()
    {
      var comps = (from comp in Base.AllComps select comp.CompInspectStringExtra() into text where !text.NullOrEmpty() select text.Replace('\n', ' ')).ToArray();
      return comps.Length > 0 ? comps.ToCommaList() : null;
    }

    private Func<string> GetBioTooltip()
    {
      if (IsAnimal) { return GetAnimalTooltip(); }

      if (Base.story == null) { return null; }

      return () =>
      {
        var builder = new StringBuilder();

        var title = Base.story?.TitleCap;
        if (title != null) { builder.TryAppendLine(Lang.Get("Model.Bio.Title", title)); }
        var faction = Base.Faction?.Name;
        if (faction != null) { builder.TryAppendLine(Lang.Get("Model.Bio.Faction", faction)); }
        var ideoligion = !ModLister.IdeologyInstalled || Base.Ideo == null ? null : Base.Ideo.name;
        if (ideoligion != null) { builder.TryAppendLine(Lang.Get("Model.Bio.Ideoligion", ideoligion)); }

        builder.AppendLine();

        var childhood = Base.story.GetBackstory(BackstorySlot.Childhood);
        var childhoodText = childhood == null ? null : $"{"Childhood".Translate()}: {childhood.TitleCapFor(Base.gender)}";
        var adulthood = Base.story.GetBackstory(BackstorySlot.Adulthood);
        var adulthoodText = adulthood == null ? null : $"{"Adulthood".Translate()}: {adulthood.TitleCapFor(Base.gender)}";

        builder.TryAppendLine(childhoodText);
        builder.TryAppendLine(adulthoodText);

        builder.AppendLine();

        var traits = Base.story.traits.allTraits.Count > 0 ? "Traits".Translate() + ": " + Base.story.traits.allTraits.Select(trait => trait.LabelCap).ToCommaList(true) : null;
        builder.TryAppendLine(traits);

        builder.AppendLine();

        var disabledWork = Base.story.DisabledWorkTagsBackstoryAndTraits;
        string incapable = disabledWork == WorkTags.None ? null : "IncapableOf".Translate() + ": " + disabledWork.GetAllSelectedItems<WorkTags>().Where(tag => tag != WorkTags.None).Select(tag => tag.LabelTranslated().CapitalizeFirst()).ToCommaList(true);

        builder.TryAppendLine(incapable.NullOrEmpty() ? null : incapable.Color(Theme.CriticalColor.Value));

        return builder.Length > 0 ? builder.ToStringTrimmed().Size(Theme.RegularTextStyle.ActualSize) : null;
      };
    }

    private TextModel GetMaster()
    {
      var master = Base.playerSettings?.Master;
      if (master == null) { return null; }

      var masterName = master.LabelShort;
      var relation = Base.GetMostImportantRelation(master)?.LabelCap;
      return TextModel.Create(Lang.Get("Model.Bio.Master", masterName), GetAnimalTooltip(), relation == null ? (Color?) null : Theme.SkillMinorPassionColor.Value, InspectPanePlus.ToggleSocialTab);
    }

    public Func<string> GetAnimalTooltip(TrainableDef def = null) => () =>
    {
      var builder = new StringBuilder();

      if (Base.RaceProps != null)
      {
        var trainability = Base.RaceProps.trainability?.LabelCap;
        if (trainability != null) { builder.AppendLine(Lang.Get("Model.Bio.Trainability", trainability)); }

        builder.AppendLine($"{"TrainingDecayInterval".Translate()}: {TrainableUtility.DegradationPeriodTicks(Base.def).ToStringTicksToDays()}");
        if (!TrainableUtility.TamenessCanDecay(Base.def)) { builder.AppendLine("TamenessWillNotDecay".Translate()); }

        builder.AppendLine(Lang.Get("Model.Bio.Petness", Base.RaceProps.petness.ToStringPercent()));
        builder.AppendLine(Lang.Get("Model.Bio.Diet", Base.RaceProps.ResolvedDietCategory.ToStringHuman()));
      }

      var master = Base.playerSettings?.Master?.LabelShort;
      if (!master.NullOrEmpty())
      {
        builder.AppendLine();
        builder.AppendLine(Lang.Get("Model.Bio.Master", master));
      }

      if (def != null)
      {
        builder.AppendLine();
        builder.AppendLine(def.description);
      }

      return builder.ToStringTrimmed().Size(Theme.RegularTextStyle.ActualSize);
    };
  }
}
