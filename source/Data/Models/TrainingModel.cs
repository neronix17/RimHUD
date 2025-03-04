﻿using System;
using RimHUD.Data.Configuration;
using RimHUD.Data.Extensions;
using RimHUD.Interface;
using RimHUD.Patch;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimHUD.Data.Models
{
  internal class TrainingModel : IValueModel
  {
    public PawnModel Model { get; }
    public bool Hidden { get; }

    public string Label { get; }
    public Color? Color { get; }
    public Func<string> Tooltip { get; }

    public Action OnHover { get; }
    public Action OnClick { get; }

    public string Value { get; }

    public TrainingModel(PawnModel model, TrainableDef def)
    {
      try
      {
        Model = model;

        bool canTrainNow;
        try { canTrainNow = model.Base.RaceProps?.trainability != null && model.Base.training != null && model.Base.training.CanAssignToTrain(def, out var visible).Accepted && visible; }
        catch (Exception exception)
        {
          Mod.HandleWarning(exception);
          canTrainNow = false;
        }

        if (!canTrainNow)
        {
          Hidden = true;
          return;
        }

        Label = def.LabelCap;

        var disabled = !model.Base.training.GetWanted(def);
        var hasLearned = model.Base.training.HasLearned(def);

        var steps = GetSteps(model.Base, def);
        var value = steps + " / " + def.steps;
        Value = hasLearned ? value.Bold() : value;

        Tooltip = model.GetAnimalTooltip(def);

        if (disabled) { Color = Theme.DisabledColor.Value; }
        else if (hasLearned) { Color = Theme.SkillMinorPassionColor.Value; }
        else { Color = Theme.MainTextColor.Value; }

        OnClick = InspectPanePlus.ToggleTrainingTab;
      }
      catch (Exception exception)
      {
        Mod.HandleWarning(exception);
        Hidden = true;
      }
    }

    private static int GetSteps(Pawn pawn, TrainableDef def) => (int) Access.Method_RimWorld_Pawn_TrainingTracker_GetSteps.Invoke(pawn.training, def);
  }
}
