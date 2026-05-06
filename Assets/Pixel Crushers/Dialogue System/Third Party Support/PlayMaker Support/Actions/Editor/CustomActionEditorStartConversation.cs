using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HutongGames.PlayMakerEditor;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    [CustomActionEditor(typeof(StartConversation))]
    public class CustomActionEditorStartConversation : CustomActionEditor
    {

        private ConversationPicker conversationPicker = null;
        private DialogueEntryPicker entryPicker = null;
        protected string[] conversationTitles = null;

        public override void OnEnable()
        {
            var action = target as StartConversation;
            if (action.conversation == null) action.conversation = new HutongGames.PlayMaker.FsmString();
            conversationPicker = new ConversationPicker(EditorTools.FindInitialDatabase(), action.conversation.Value, !action.conversation.UseVariable);
        }

        public override bool OnGUI()
        {
            var isDirty = false;

            var action = target as StartConversation;
            if (action == null) return DrawDefaultInspector();

            EditorGUI.BeginChangeCheck();

            if (action.conversation == null) action.conversation = new HutongGames.PlayMaker.FsmString();
            action.conversation.UseVariable = EditorGUILayout.Toggle(new GUIContent("Use Variable", "Specify the conversation title with a PlayMaker variable"), action.conversation.UseVariable);

            if (action.conversation.UseVariable)
            {
                EditField("conversation");
            }
            else
            {
                conversationPicker.Draw(true);
                if (!string.Equals(action.conversation.Value, conversationPicker.currentConversation))
                {
                    action.conversation.Value = conversationPicker.currentConversation;
                    isDirty = true;
                    entryPicker = null;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                conversationTitles = null;
            }

            if (action.startingEntryID == null)
            {
                action.startingEntryID = new HutongGames.PlayMaker.FsmInt();
                action.startingEntryID.Value = -1;
            }
            var specifyEntryID = (action.startingEntryID.Value != -1);
            EditorGUI.BeginChangeCheck();
            specifyEntryID = EditorGUILayout.Toggle(new GUIContent("Specify Starting Entry ID", "Specify a dialogue entry at which to start the conversation"), specifyEntryID);
            if (EditorGUI.EndChangeCheck())
            {
                action.startingEntryID.Value = specifyEntryID ? 0 : -1;
            }
            if (specifyEntryID)
            {
                action.startingEntryTitle.Value = string.Empty;
                if (entryPicker == null)
                {
                    entryPicker = new DialogueEntryPicker(action.conversation.Value);
                }
                if (entryPicker.isValid)
                {
                    action.startingEntryID.Value = entryPicker.DoLayout("Starting Entry ID", action.startingEntryID.Value);
                }
                else
                {
                    EditField("startingEntryID");
                }
            }

            var specifyEntryTitle = action.startingEntryTitle.UseVariable || !string.IsNullOrEmpty(action.startingEntryTitle.Value);
            EditorGUI.BeginChangeCheck();
            specifyEntryTitle = EditorGUILayout.Toggle(new GUIContent("Specify Starting Entry Title", "Specify a dialogue entry at which to start the conversation"), specifyEntryTitle);
            if (EditorGUI.EndChangeCheck())
            {
                if (!specifyEntryTitle) action.startingEntryTitle.Value = string.Empty;
            }

            if (specifyEntryTitle)
            {
                action.startingEntryID.Value = -1;

                if (action.startingEntryTitle == null) action.startingEntryTitle = new HutongGames.PlayMaker.FsmString();
                action.startingEntryTitle.UseVariable = EditorGUILayout.Toggle(new GUIContent("Use Variable", "Specify the dialogue entry title with a PlayMaker variable"), action.startingEntryTitle.UseVariable);

                if (action.startingEntryTitle.UseVariable)
                {
                    EditField("startingEntryTitle");
                }
                else
                {
                    // Draw entry title picker:
                    if (conversationTitles == null)
                    {
                        conversationTitles = GetUniqueTitles(action.conversation.Value);
                    }
                    if (string.IsNullOrEmpty(action.startingEntryTitle.Value))
                    {
                        action.startingEntryTitle = conversationTitles[0];
                    }
                    var titleIndex = GetTitleIndex(conversationTitles, action.startingEntryTitle.Value);
                    if (titleIndex == -1) titleIndex = 0;
                    EditorGUI.BeginChangeCheck();
                    titleIndex = EditorGUILayout.Popup(new GUIContent("Starting Entry Title", "Start at entry with this Title. If set, takes precedence over Starting Entry ID"), titleIndex, conversationTitles);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (0 <= titleIndex && titleIndex < conversationTitles.Length)
                        {
                            action.startingEntryTitle.Value = conversationTitles[titleIndex];
                        }
                    }
                }
            }

            EditField("actor");
            EditField("conversant");
            EditField("exclusive");
            EditField("replace");

            return isDirty || GUI.changed;
        }

        protected string[] GetUniqueTitles(string conversationTitle)
        {
            var list = new List<string>();
            if (conversationPicker.database != null)
            {
                var conversation = conversationPicker.database.GetConversation(conversationTitle);
                if (conversation != null)
                {
                    foreach (var entry in conversation.dialogueEntries)
                    {
                        var title = entry.Title;
                        if (!list.Contains(title))
                        {
                            list.Add(title);
                        }
                    }
                }
            }
            return list.ToArray();
        }

        protected int GetTitleIndex(string[] titles, string currentTitle)
        {
            if (string.IsNullOrEmpty(currentTitle) || titles == null) return -1;
            for (int i = 0; i < titles.Length; i++)
            {
                if (string.Equals(currentTitle, titles[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        protected int GetEntryIDFromTitle(string conversationTitle, string entryTitle)
        {
            if (conversationPicker.database != null)
            {
                var conversation = conversationPicker.database.GetConversation(conversationTitle);
                if (conversation != null)
                {
                    foreach (var entry in conversation.dialogueEntries)
                    {
                        if (entry.Title == entryTitle) return entry.id;
                    }
                }
            }
            return -1;
        }

    }
}