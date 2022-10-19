﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.XPath;
using OtterGui;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization;

// Each Subrace and Gender combo has a customization set.
// This describes the available customizations, their types and their names.
public class CustomizationSet
{
    internal CustomizationSet(SubRace clan, Gender gender)
    {
        Gender            = gender;
        Clan              = clan;
        Race              = clan.ToRace();
        _settingAvailable = 0;
    }

    public Gender  Gender { get; }
    public SubRace Clan   { get; }
    public Race    Race   { get; }

    private CustomizeFlag _settingAvailable;

    internal void SetAvailable(CustomizeIndex index)
        => _settingAvailable |= index.ToFlag();

    public bool IsAvailable(CustomizeIndex index)
        => _settingAvailable.HasFlag(index.ToFlag());

    // Meta
    public IReadOnlyList<string> OptionName { get; internal set; } = null!;

    public string Option(CustomizeIndex index)
        => OptionName[(int)index];

    public IReadOnlyList<CharaMakeParams.MenuType>                         Types { get; internal set; } = null!;
    public IReadOnlyDictionary<CharaMakeParams.MenuType, CustomizeIndex[]> Order { get; internal set; } = null!;


    // Always list selector.
    public int NumEyebrows    { get; internal init; }
    public int NumEyeShapes   { get; internal init; }
    public int NumNoseShapes  { get; internal init; }
    public int NumJawShapes   { get; internal init; }
    public int NumMouthShapes { get; internal init; }


    // Always Icon Selector
    public IReadOnlyList<CustomizeData>                  Faces          { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData>                  HairStyles     { get; internal init; } = null!;
    public IReadOnlyList<IReadOnlyList<CustomizeData>>   HairByFace     { get; internal set; }  = null!;
    public IReadOnlyList<CustomizeData>                  TailEarShapes  { get; internal init; } = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature1 { get; internal set; }  = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature2 { get; internal set; }  = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature3 { get; internal set; }  = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature4 { get; internal set; }  = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature5 { get; internal set; }  = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature6 { get; internal set; }  = null!;
    public IReadOnlyList<(CustomizeData, CustomizeData)> FacialFeature7 { get; internal set; }  = null!;
    public (CustomizeData, CustomizeData)                LegacyTattoo   { get; internal set; }
    public IReadOnlyList<CustomizeData>                  FacePaints     { get; internal init; } = null!;


    // Always Color Selector
    public IReadOnlyList<CustomizeData> SkinColors           { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> HairColors           { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> HighlightColors      { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> EyeColors            { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> TattooColors         { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> FacePaintColorsLight { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> FacePaintColorsDark  { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> LipColorsLight       { get; internal init; } = null!;
    public IReadOnlyList<CustomizeData> LipColorsDark        { get; internal init; } = null!;


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int DataByValue(CustomizeIndex index, CustomizeValue value, out CustomizeData? custom, CustomizeValue face)
    {
        var type = Types[(int)index];

        int GetInteger(out CustomizeData? custom)
        {
            if (value < Count(index))
            {
                custom = new CustomizeData(index, value, 0, value.Value);
                return value.Value;
            }

            custom = null;
            return -1;
        }

        static int GetBool(CustomizeIndex index, CustomizeValue value, out CustomizeData? custom)
        {
            if (value == CustomizeValue.Zero)
            {
                custom = new CustomizeData(index, CustomizeValue.Zero, 0, 0);
                return 0;
            }

            var (_, mask) = index.ToByteAndMask();
            if (value.Value == mask)
            {
                custom = new CustomizeData(index, new CustomizeValue(mask), 0, 1);
                return 1;
            }

            custom = null;
            return -1;
        }

        static int Invalid(out CustomizeData? custom)
        {
            custom = null;
            return -1;
        }

        int Get(IEnumerable<CustomizeData> list, CustomizeValue v, out CustomizeData? output)
        {
            var (val, idx) = list.Cast<CustomizeData?>().WithIndex().FirstOrDefault(p => p.Item1!.Value.Value == v);
            if (val == null)
            {
                output = null;
                return -1;
            }

            output = val;
            return idx;
        }

        return type switch
        {
            CharaMakeParams.MenuType.ListSelector => GetInteger(out custom),
            CharaMakeParams.MenuType.IconSelector => index switch
            {
                CustomizeIndex.Face      => Get(Faces, HrothgarFaceHack(value), out custom),
                CustomizeIndex.Hairstyle => Get((face = HrothgarFaceHack(face)).Value < HairByFace.Count ? HairByFace[face.Value] : HairStyles, value, out custom),
                CustomizeIndex.TailShape => Get(TailEarShapes, value, out custom),
                CustomizeIndex.FacePaint => Get(FacePaints, value, out custom),
                CustomizeIndex.LipColor  => Get(LipColorsDark, value, out custom),
                _                        => Invalid(out custom),
            },
            CharaMakeParams.MenuType.ColorPicker => index switch
            {
                CustomizeIndex.SkinColor       => Get(SkinColors,                                       value, out custom),
                CustomizeIndex.EyeColorLeft    => Get(EyeColors,                                        value, out custom),
                CustomizeIndex.EyeColorRight   => Get(EyeColors,                                        value, out custom),
                CustomizeIndex.HairColor       => Get(HairColors,                                       value, out custom),
                CustomizeIndex.HighlightsColor => Get(HighlightColors,                                  value, out custom),
                CustomizeIndex.TattooColor     => Get(TattooColors,                                     value, out custom),
                CustomizeIndex.LipColor        => Get(LipColorsDark.Concat(LipColorsLight),             value, out custom),
                CustomizeIndex.FacePaintColor  => Get(FacePaintColorsDark.Concat(FacePaintColorsLight), value, out custom),
                _                              => Invalid(out custom),
            },
            CharaMakeParams.MenuType.DoubleColorPicker => index switch
            {
                CustomizeIndex.LipColor       => Get(LipColorsDark.Concat(LipColorsLight),             value, out custom),
                CustomizeIndex.FacePaintColor => Get(FacePaintColorsDark.Concat(FacePaintColorsLight), value, out custom),
                _                             => Invalid(out custom),
            },
            CharaMakeParams.MenuType.IconCheckmark => GetBool(index, value, out custom),
            CharaMakeParams.MenuType.Percentage    => GetInteger(out custom),
            CharaMakeParams.MenuType.Checkmark     => GetBool(index, value, out custom),
            _                                      => Invalid(out custom),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public CustomizeData Data(CustomizeIndex index, int idx)
        => Data(index, idx, CustomizeValue.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public CustomizeData Data(CustomizeIndex index, int idx, CustomizeValue face)
    {
        if (idx >= Count(index, face = HrothgarFaceHack(face)))
            throw new IndexOutOfRangeException();

        switch (Types[(int)index])
        {
            case CharaMakeParams.MenuType.Percentage:   return new CustomizeData(index, (CustomizeValue)idx,           0, (ushort)idx);
            case CharaMakeParams.MenuType.ListSelector: return new CustomizeData(index, (CustomizeValue)idx,           0, (ushort)idx);
            case CharaMakeParams.MenuType.Checkmark:    return new CustomizeData(index, CustomizeValue.Bool(idx != 0), 0, (ushort)idx);
        }

        return index switch
        {
            CustomizeIndex.Face            => Faces[idx],
            CustomizeIndex.Hairstyle       => face < HairByFace.Count ? HairByFace[face.Value][idx] : HairStyles[idx],
            CustomizeIndex.TailShape       => TailEarShapes[idx],
            CustomizeIndex.FacePaint       => FacePaints[idx],
            CustomizeIndex.FacialFeature1  => idx == 0 ? FacialFeature1[face.Value].Item1 : FacialFeature1[face.Value].Item2,
            CustomizeIndex.FacialFeature2  => idx == 0 ? FacialFeature2[face.Value].Item1 : FacialFeature2[face.Value].Item2,
            CustomizeIndex.FacialFeature3  => idx == 0 ? FacialFeature3[face.Value].Item1 : FacialFeature3[face.Value].Item2,
            CustomizeIndex.FacialFeature4  => idx == 0 ? FacialFeature4[face.Value].Item1 : FacialFeature4[face.Value].Item2,
            CustomizeIndex.FacialFeature5  => idx == 0 ? FacialFeature5[face.Value].Item1 : FacialFeature5[face.Value].Item2,
            CustomizeIndex.FacialFeature6  => idx == 0 ? FacialFeature6[face.Value].Item1 : FacialFeature6[face.Value].Item2,
            CustomizeIndex.FacialFeature7  => idx == 0 ? FacialFeature7[face.Value].Item1 : FacialFeature7[face.Value].Item2,
            CustomizeIndex.LegacyTattoo    => idx == 0 ? LegacyTattoo.Item1 : LegacyTattoo.Item2,
            CustomizeIndex.SkinColor       => SkinColors[idx],
            CustomizeIndex.EyeColorLeft    => EyeColors[idx],
            CustomizeIndex.EyeColorRight   => EyeColors[idx],
            CustomizeIndex.HairColor       => HairColors[idx],
            CustomizeIndex.HighlightsColor => HighlightColors[idx],
            CustomizeIndex.TattooColor     => TattooColors[idx],
            CustomizeIndex.LipColor        => idx < 96 ? LipColorsDark[idx] : LipColorsLight[idx - 96],
            CustomizeIndex.FacePaintColor  => idx < 96 ? FacePaintColorsDark[idx] : FacePaintColorsLight[idx - 96],
            _                              => new CustomizeData(0, CustomizeValue.Zero),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public CharaMakeParams.MenuType Type(CustomizeIndex index)
        => Types[(int)index];

    internal static IReadOnlyDictionary<CharaMakeParams.MenuType, CustomizeIndex[]> ComputeOrder(CustomizationSet set)
    {
        var ret = Enum.GetValues<CustomizeIndex>().SkipLast(1).ToArray();
        ret[(int)CustomizeIndex.TattooColor]   = CustomizeIndex.EyeColorLeft;
        ret[(int)CustomizeIndex.EyeColorLeft]  = CustomizeIndex.EyeColorRight;
        ret[(int)CustomizeIndex.EyeColorRight] = CustomizeIndex.TattooColor;

        var dict = ret.Skip(2).Where(set.IsAvailable).GroupBy(set.Type).ToDictionary(k => k.Key, k => k.ToArray());
        foreach (var type in Enum.GetValues<CharaMakeParams.MenuType>())
            dict.TryAdd(type, Array.Empty<CustomizeIndex>());
        return dict;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int Count(CustomizeIndex index)
        => Count(index, CustomizeValue.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public int Count(CustomizeIndex index, CustomizeValue face)
    {
        if (!IsAvailable(index))
            return 0;

        return Type(index) switch
        {
            CharaMakeParams.MenuType.Percentage    => 101,
            CharaMakeParams.MenuType.IconCheckmark => 2,
            CharaMakeParams.MenuType.Checkmark     => 2,
            _ => index switch
            {
                CustomizeIndex.Face            => Faces.Count,
                CustomizeIndex.Hairstyle       => (face = HrothgarFaceHack(face)) < HairByFace.Count ? HairByFace[face.Value].Count : 0,
                CustomizeIndex.SkinColor       => SkinColors.Count,
                CustomizeIndex.EyeColorRight   => EyeColors.Count,
                CustomizeIndex.HairColor       => HairColors.Count,
                CustomizeIndex.HighlightsColor => HighlightColors.Count,
                CustomizeIndex.TattooColor     => TattooColors.Count,
                CustomizeIndex.Eyebrows        => NumEyebrows,
                CustomizeIndex.EyeColorLeft    => EyeColors.Count,
                CustomizeIndex.EyeShape        => NumEyeShapes,
                CustomizeIndex.Nose            => NumNoseShapes,
                CustomizeIndex.Jaw             => NumJawShapes,
                CustomizeIndex.Mouth           => NumMouthShapes,
                CustomizeIndex.LipColor        => LipColorsLight.Count + LipColorsDark.Count,
                CustomizeIndex.TailShape       => TailEarShapes.Count,
                CustomizeIndex.FacePaint       => FacePaints.Count,
                CustomizeIndex.FacePaintColor  => FacePaintColorsLight.Count + FacePaintColorsDark.Count,
                _                              => throw new ArgumentOutOfRangeException(nameof(index), index, null),
            },
        };
    }

    private CustomizeValue HrothgarFaceHack(CustomizeValue value)
        => Race == Race.Hrothgar && value.Value is > 4 and < 9 ? value - 4 : value;
}
