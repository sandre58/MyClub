﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MyClub.Scorer.Wpf.Settings {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.8.0.0")]
    internal sealed partial class ExportTeamsSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static ExportTeamsSettings defaultInstance = ((ExportTeamsSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new ExportTeamsSettings())));
        
        public static ExportTeamsSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string Folder {
            get {
                return ((string)(this["Folder"]));
            }
            set {
                this["Folder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string SelectedColumns {
            get {
                return ((string)(this["SelectedColumns"]));
            }
            set {
                this["SelectedColumns"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string ColumnsOrder {
            get {
                return ((string)(this["ColumnsOrder"]));
            }
            set {
                this["ColumnsOrder"] = value;
            }
        }
    }
}
