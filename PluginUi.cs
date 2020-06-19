﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud;
using ImGuiNET;
using TeleporterPlugin.Managers;
using TeleporterPlugin.Objects;

namespace TeleporterPlugin {
    public class PluginUi : IDisposable {
        private readonly TeleporterPlugin _plugin;
        public bool DebugVisible;
        private bool _settingsVisible;
        public bool SettingsVisible {
            get => _settingsVisible;
            set {
                if (value) LoadSettings();
                _settingsVisible = value;
            }
        }

        public PluginUi(TeleporterPlugin plugin) {
            _plugin = plugin;
            LoadSettings();
            _plugin.Interface.UiBuilder.OnBuildUi += Draw;
            _plugin.Interface.UiBuilder.OnOpenConfigUi += (sender, args) => SettingsVisible = true;
        }

        private void Draw() {
            if (DebugVisible) DrawDebug();
            if (_settingsVisible) DrawSettings();
        }
        
        private void LoadSettings() {
            cfg_inputGilThreshold = _plugin.Config.GilThreshold;
            cfg_useGilThreshold = _plugin.Config.UseGilThreshold;
            cfg_tpTypeSelect = (int)_plugin.Config.DefaultTeleportType;
            cfg_skipTicketPopup = _plugin.Config.SkipTicketPopup;
            cfg_aliasList = new List<TeleportAlias>(_plugin.Config.AliasList.Select(a => new TeleportAlias(a.Alias, a.Aetheryte)));
            cfg_aetheryteList = TeleportManager.AetheryteList.Select(a => a.Name).ToArray();
        }

        private void SaveSettings() {
            _plugin.Config.GilThreshold = cfg_inputGilThreshold;
            _plugin.Config.SkipTicketPopup = cfg_skipTicketPopup;
            _plugin.Config.UseGilThreshold = cfg_useGilThreshold;
            _plugin.Config.DefaultTeleportType = (TeleportType)cfg_tpTypeSelect;
            _plugin.Config.AliasList = new List<TeleportAlias>(cfg_aliasList.Select(a => new TeleportAlias(a.Alias, a.Aetheryte)));
            _plugin.Config.Save();
        }

        #region Settings Window

        private static readonly Vector4 ColorRed = new Vector4(255, 0, 0, 255);
        private static readonly string[] TpTypesList = Enum.GetNames(typeof(TeleportType));

        private int cfg_tpTypeSelect;
        private bool cfg_useGilThreshold;
        private int cfg_inputGilThreshold;
        private bool cfg_skipTicketPopup;
        private List<TeleportAlias> cfg_aliasList;
        private string[] cfg_aetheryteList;

        public void DrawSettings() {
            ImGui.SetNextWindowSize(new Vector2(530, 450), ImGuiCond.Appearing);
            ImGui.SetNextWindowSizeConstraints(new Vector2(300, 300), new Vector2(float.MaxValue, float.MaxValue));
            if (!ImGui.Begin($"{_plugin.Name} Config", ref _settingsVisible, ImGuiWindowFlags.NoScrollWithMouse)) return;

            if (ImGui.BeginChild("##scollingregionSettings", new Vector2(0, -ImGui.GetTextLineHeight() * 2))) {
                if (ImGui.CollapsingHeader("General Settings", ImGuiTreeNodeFlags.DefaultOpen))
                    DrawGeneral();

                if (ImGui.CollapsingHeader("Alias Settings"))
                    DrawAlias();

                ImGui.EndChild();
            }

            DrawFooter();
            ImGui.End();
        }

        private void DrawAlias() {
            var newAliasAdded = false;
            if (ImGui.Button("New Alias")) {
                cfg_aliasList.Insert(0, TeleportAlias.Empty);
                newAliasAdded = true;
            }
            ImGui.SameLine();
            ImGui.Spacing();
            //ImGui.TextUnformatted("");
            ImGui.SameLine();
            if (ImGui.Button("Delete")) {
                if (cfg_aliasList.Count > 0)
                    cfg_aliasList.RemoveAt(0);
            }
            var deleteAliasHovered = ImGui.IsItemHovered();
            
            ImGui.SameLine();
            if (ImGui.Button("Delete Selected"))
                cfg_aliasList.RemoveAll(a => a.GuiSelected);

            ImGui.SameLine();
            if (ImGui.Button("Delete All"))
                ImGui.OpenPopupOnItemClick("deleteallpopup", 0);

            if (ImGui.BeginPopup("deleteallpopup")) {
                ImGui.TextColored(ColorRed, "Are you sure you want to delete ALL aliases?");
                if (ImGui.Button("No", new Vector2(80, ImGui.GetTextLineHeightWithSpacing()))) {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine(ImGui.GetWindowWidth() - 90);
                if (ImGui.Button("Yes", new Vector2(80, ImGui.GetTextLineHeightWithSpacing()))) {
                    cfg_aliasList.Clear();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            
            ImGui.Separator();
            if (!ImGui.BeginChild("##scrollingregionAlias", Vector2.Zero)) 
                return;
            if (newAliasAdded) ImGui.SetScrollHereY();
            ImGui.Columns(2);
            ImGui.TextUnformatted("Alias"); ImGui.NextColumn();
            ImGui.TextUnformatted("Target Aetheryte"); ImGui.NextColumn();
            ImGui.Separator();

            for (var i = 0; i < cfg_aliasList.Count; i++) {
                var alias = cfg_aliasList[i];
                if (deleteAliasHovered && i == 0) ImGui.ArrowButton("delete_indicator", ImGuiDir.Right);
                else ImGui.Checkbox($"##hidelabelAliasSelected{i}", ref alias.GuiSelected);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth() - 45);
                if (deleteAliasHovered && i == 0) ImGui.TextColored(ColorRed, alias.Alias);
                else ImGui.InputText($"##hidelabelAliasKey{i}", alias.AliasBuffer, TeleportAlias.BufferSize, ImGuiInputTextFlags.CharsNoBlank);
                ImGui.NextColumn();
                if (deleteAliasHovered && i == 0) ImGui.TextColored(ColorRed, alias.Aetheryte);
                else {
                    ImGui.SetNextItemWidth(ImGui.GetColumnWidth() - 45);
                    ImGui.InputText($"##hidelabelAliasValue{i}", alias.AetheryteBuffer, TeleportAlias.BufferSize);
                    ImGui.SameLine();
                    if (ImGui.BeginCombo($"##hidelabelAliasSelect{i}", "", ImGuiComboFlags.NoPreview)) {
                        for (var a = 0; a < cfg_aetheryteList.Length; a++) {
                            var selected = alias.GuiSelectedIndex == a;
                            if (ImGui.Selectable(cfg_aetheryteList[a], selected)) {
                                alias.GuiSelectedIndex = a;
                                alias.Aetheryte = cfg_aetheryteList[a];
                            }
                            if(selected) ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }
                }
                ImGui.NextColumn();
            }
            
            ImGui.EndChild();
            ImGui.Columns(1);
        }

        private void DrawGeneral() {
            ImGui.TextUnformatted("Default Teleport Type:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            ImGui.Combo("##hidelabelTypeCombo", ref cfg_tpTypeSelect, TpTypesList, TpTypesList.Length);
            ImGui.Checkbox("Skip Ticket Popup", ref cfg_skipTicketPopup);
            ImGui.Checkbox("Use Tickets if Gil Price is above:", ref cfg_useGilThreshold);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.InputInt("##hidelabelInputGil", ref cfg_inputGilThreshold, 1, 100) && cfg_inputGilThreshold < 0) cfg_inputGilThreshold = 0;
        }

        private void DrawFooter() {
            ImGui.Separator();
            if (ImGui.Button("Save")) 
                SaveSettings();
            ImGui.SameLine();
            if (ImGui.Button("Save and Close")) {
                SaveSettings();
                SettingsVisible = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Close")) SettingsVisible = false;
        }

        #endregion

        #region Debug Window

        private readonly List<string> dbg_locations = new List<string> {"GetList to Update"};
        private readonly List<string> dbg_aetheryteId = new List<string> {"Empty"};
        private readonly List<string> dbg_subIndex = new List<string> {"Empty"};
        private readonly List<string> dbg_zoneId = new List<string> {"Empty"};
        private int dbg_selected;
        private readonly string[] dbg_langs = Enum.GetNames(typeof(ClientLanguage));
        private int dbg_selectedLang = (int)TeleportManager.CurrentLanguage;

        public void DrawDebug() {
            var windowSize = new Vector2(350, 315);
            ImGui.SetNextWindowSize(windowSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(windowSize, new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin($"{_plugin.Name} Debug", ref DebugVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                ImGui.TextUnformatted($"AetheryteList: {TeleportManager.AetheryteListAddress.ToInt64():X8}");
                ImGui.TextUnformatted($"TeleportStatus: {TeleportManager.TeleportStatusAddress.ToInt64():X8}");
                ImGui.TextUnformatted($"ItemCountStaticArg: {TeleportManager.ItemCountStaticArgAddress.ToInt64():X8}");
                ImGui.TextUnformatted("Language:"); ImGui.SameLine();
                if (ImGui.Combo("##hidelabelLangCombo", ref dbg_selectedLang, dbg_langs, dbg_langs.Length)) {
                    TeleportManager.DebugSetLanguage((ClientLanguage)dbg_selectedLang, _plugin.Interface);
                }
                ImGui.Separator();
                
                if (ImGui.Button("GetList")) {
                    dbg_locations.Clear();
                    dbg_aetheryteId.Clear();
                    dbg_subIndex.Clear();
                    dbg_zoneId.Clear();
                    foreach (var location in TeleportManager.AetheryteList) {
                        dbg_locations.Add(location.Name);
                        dbg_aetheryteId.Add(location.AetheryteId.ToString());
                        dbg_subIndex.Add(location.SubIndex.ToString());
                        dbg_zoneId.Add(location.ZoneId.ToString());
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("Teleport")) 
                    TeleportManager.Teleport(dbg_locations[dbg_selected]);
                ImGui.SameLine();
                if (ImGui.Button("Teleport (Ticket)")) 
                    TeleportManager.TeleportTicket(dbg_locations[dbg_selected], true);
                ImGui.SameLine();
                if (ImGui.Button("Teleport (Ticket + Popup)"))
                    TeleportManager.TeleportTicket(dbg_locations[dbg_selected]);

                ImGui.BeginChild("##scrollingregion");
                ImGui.Columns(4, "##listbox");
                ImGui.SetColumnWidth(1, 50);
                ImGui.SetColumnWidth(2, 80);
                ImGui.SetColumnWidth(3, 80);
                ImGui.Separator();
                ImGui.Text("Name"); ImGui.NextColumn();
                ImGui.Text("Id"); ImGui.NextColumn();
                ImGui.Text("SubIndex"); ImGui.NextColumn();
                ImGui.Text("ZoneId"); ImGui.NextColumn();

                ImGui.Separator();
                for (var i = 0; i < dbg_locations.Count; i++) {
                    if (ImGui.Selectable($"{dbg_locations[i]}", dbg_selected == i, ImGuiSelectableFlags.SpanAllColumns))
                        dbg_selected = i;
                    ImGui.NextColumn();
                    ImGui.Text(dbg_aetheryteId[i]); ImGui.NextColumn();
                    ImGui.Text(dbg_subIndex[i]); ImGui.NextColumn();
                    ImGui.Text(dbg_zoneId[i]); ImGui.NextColumn();
                }

                ImGui.EndChild();
                ImGui.Columns(1);
                ImGui.Separator();
            }
            ImGui.End();
        }

        #endregion

        public void Dispose() {
            _plugin.Interface.UiBuilder.OnBuildUi -= Draw;
        }
    }
}