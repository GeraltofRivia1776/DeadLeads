using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

public class NecroNotes : EditorWindow
{
    private string versionNumber = "v2.5";
    private Texture2D logoTexture;
    private List<Note> notes = new List<Note>();
    private Vector2 scrollPosition;
    private float padding = 2f; // Padding for the sides and bottom
    private float minEditorWidth = 232f; // Minimum width including note padding
    private Color selectedColor = Color.green; // Default color
    public Color editorBackgroundColor = new Color(0.8f, 0.8f, 0.8f, 1.0f); // Default to light gray
    private Color textColor = Color.white; // Default text color

    private int selectedNoteIndex = -1; // Initialize as -1 to indicate no note is selected
    private bool showColorPicker = true; // Flag to track the visibility of the color picker section

    public enum LogoSize { Small, Medium, Large }
    private LogoSize currentLogoSize = LogoSize.Large;


    [System.Serializable]
    public class Note
    {
        public string title = "New Note";
        public string content;
        public Rect rect;
        public string tempTitleInput = "";
        public Color color = Color.white; // Color for the note

        public Note() { } // Required for serialization

        public Note(float x, float y, float width, float height, string content, Color color)
        {
            rect = new Rect(x, y, width, height);
            this.content = content;
            this.color = new Color(color.r, color.g, color.b, 1f); // Ensure alpha is set to 1f
        }
    }

    [MenuItem("Tools/Necro Notes")]
    public static void ShowWindow()
    {
        NecroNotes window = GetWindow<NecroNotes>("Necro Notes");
        window.minSize = new Vector2(window.minEditorWidth, 300f); // Adjust the minimum height as needed
    }

    private void OnEnable()
    {
        LoadEditorBackgroundColor();
        LoadNotes();
        logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/NecroNotes/NecroNotes.png");

        // Subscribe to EditorApplication callbacks
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.quitting += SaveNotes;
    }

    private void OnDisable()
    {
        SaveEditorBackgroundColor();
        SaveNotes();
        // Unsubscribe from EditorApplication callbacks
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.quitting -= SaveNotes;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Save notes when entering or exiting play mode
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
        {
            SaveNotes();
        }
        // Load notes when entering play mode
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            LoadNotes();
        }
    }

    private void OnGUI()
    {
        selectedColor = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 1f); // Ensure alpha is set to 1f

        float topAreaHeight = GetDynamicTopAreaHeight(); // Calculate dynamic height

        // Draw the top part of the editor window with a different background color
        Rect topRect = new Rect(0, 0, position.width, topAreaHeight); // Use dynamic height
        EditorGUI.DrawRect(topRect, new Color(0.2f, 0.2f, 0.2f)); // Dark grey color

        // Draw the rest of the editor window below the top section
        Rect notesRect = new Rect(0, topRect.height, position.width, position.height - topRect.height);
        EditorGUI.DrawRect(notesRect, editorBackgroundColor); // Use the default background color for the rest of the window

        Rect versionRect = new Rect(position.width - 60, 0, 60, 20);
        GUIStyle versionStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.UpperRight };
        EditorGUI.LabelField(versionRect, versionNumber, versionStyle);

        Vector2 logoSize;
        Texture displayTexture = null; // Use Texture type to accommodate both Texture2D and GUIContent icon
        GUIContent iconContent = null; // For built-in icon
        switch (currentLogoSize)
        {
            case LogoSize.Small:
                logoSize = new Vector2(60, 60); // Typical size for an editor icon
                displayTexture = logoTexture; // Use the main logo
                break;
            case LogoSize.Medium:
                logoSize = new Vector2(90, 90); // Medium size
                displayTexture = logoTexture; // Use the main logo
                break;
            case LogoSize.Large:
            default:
                logoSize = new Vector2(120, 120); // Large size
                displayTexture = logoTexture; // Use the main logo
                break;
        }
        // Now let's define a GUIStyle to adjust the size of the icon
        GUIStyle iconStyle = new GUIStyle(GUI.skin.button); // You can use any other GUIStyle depending on your requirements
        iconStyle.fixedWidth = logoSize.x;
        iconStyle.fixedHeight = logoSize.y;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        Rect logoRect = GUILayoutUtility.GetRect(logoSize.x, logoSize.y);

        if (currentLogoSize == LogoSize.Small && iconContent != null)
        {
            // Draw the built-in icon when small
            GUI.Label(logoRect, iconContent);
        }
        else if (displayTexture != null)
        {
            // Draw the custom texture for Medium and Large sizes
            GUI.DrawTexture(logoRect, displayTexture, ScaleMode.ScaleToFit);
        }

        // Click detection and size toggling logic remains the same...
        Event e = Event.current;
        if (e.type == EventType.MouseDown && logoRect.Contains(e.mousePosition))
        {
            switch (currentLogoSize)
            {
                case LogoSize.Small:
                    currentLogoSize = LogoSize.Medium;
                    break;
                case LogoSize.Medium:
                    currentLogoSize = LogoSize.Large;
                    break;
                case LogoSize.Large:
                    currentLogoSize = LogoSize.Small;
                    break;
            }
            Repaint(); // Ensure the editor window is refreshed
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        // Create and configure a GUIStyle for bold buttons with the selected color
        GUIStyle boldButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 11, // Adjust the font size as needed
        };

        // Define a GUIStyle for bold text labels
        GUIStyle boldLabelStyle = new GUIStyle(EditorStyles.label);
        boldLabelStyle.fontStyle = FontStyle.Bold;


        // Draw the color picker section only if the flag is true
        if (showColorPicker)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Note Color", EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUI.BeginChangeCheck();
            selectedColor = EditorGUILayout.ColorField(selectedColor);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateButtonColors(selectedColor);
                SaveNotes();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Background Color", EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUI.BeginChangeCheck();
            editorBackgroundColor = EditorGUILayout.ColorField(editorBackgroundColor, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                UpdateButtonColors(editorBackgroundColor);
                SaveNotes();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Text Color", EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUI.BeginChangeCheck();
            textColor = EditorGUILayout.ColorField(textColor, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                SaveNotes();
            }
            EditorGUILayout.EndHorizontal();
        }

        // Calculate the available width for buttons considering the padding on both sides and between buttons
        float horizontalPadding = padding * 4; // Total horizontal padding (left, right, and between buttons)
        float totalAvailableWidth = position.width - horizontalPadding;
        float buttonWidth = totalAvailableWidth / 2; // Divide by the number of buttons per row

        GUILayout.BeginHorizontal();
        GUILayout.Space(padding); // Padding on the left side
        // Add button for exporting notes
        if (GUILayout.Button("Export Notes", boldButtonStyle, GUILayout.Height(30), GUILayout.Width(buttonWidth)))
        {
            ExportNotes();
            SaveNotes();
        }

        // Add button for importing notes
        if (GUILayout.Button("Import Notes", boldButtonStyle, GUILayout.Height(30), GUILayout.Width(buttonWidth)))
        {
            ImportNotes();
            SaveNotes();
        }
        GUILayout.Space(padding); // Padding on the left side
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Space(padding); // Padding on the left side
        if (GUILayout.Button("Create Note", boldButtonStyle, GUILayout.Height(30), GUILayout.Width(buttonWidth)))
        {
            notes.Add(new Note(padding + 5f, 10, 200, 100, "", selectedColor));
            SaveNotes();
        }

        if (GUILayout.Button("Delete All Notes", boldButtonStyle, GUILayout.Height(30), GUILayout.Width(buttonWidth)))
        {
            if (notes.Count > 0)
            {
                if (EditorUtility.DisplayDialog("Delete All Notes", "Are you sure you want to delete all notes?", "Yes", "No"))
                {
                    notes.Clear();
                    SaveNotes();
                }
            }
            else
            {
                // Display a message indicating there are no notes to delete
                EditorUtility.DisplayDialog("No Notes", "There are no notes to delete.", "OK");
            }
        }
        GUILayout.Space(padding); // Padding on the left side
        GUILayout.EndHorizontal();

        // Calculate the selected color based on the note's color
        Color selectedNoteColor = Color.white; // Default selected color
        if (selectedNoteIndex != -1 && selectedNoteIndex < notes.Count)
        {
            selectedNoteColor = notes[selectedNoteIndex].color;
        }

        float visibleAreaHeight = position.height - 130; // Adjusted to include padding
        float visibleAreaWidth = position.width - padding * 2; // Adjusted to include padding

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(visibleAreaHeight), GUILayout.Width(visibleAreaWidth));

        float topPadding = 3f; // Define your desired top padding

        BeginWindows();
        for (int i = 0; i < notes.Count; i++)
        {
            // Ensure there's a top padding for the first note or when any note's y position is too high
            if (notes[i].rect.y < scrollPosition.y + topPadding)
            {
                notes[i].rect.y = scrollPosition.y + topPadding;
            }

            if (notes[i].rect.yMax > scrollPosition.y + visibleAreaHeight)
            {
                notes[i].rect.y = scrollPosition.y + visibleAreaHeight - notes[i].rect.height;
            }
            else if (notes[i].rect.y < scrollPosition.y)
            {
                notes[i].rect.y = scrollPosition.y;
            }

            notes[i].rect.x = Mathf.Max(padding + 2f, notes[i].rect.x);

            if (notes[i].rect.xMax > scrollPosition.x + visibleAreaWidth)
            {
                notes[i].rect.x = scrollPosition.x + visibleAreaWidth - notes[i].rect.width;
            }
            else if (notes[i].rect.x < scrollPosition.x)
            {
                notes[i].rect.x = scrollPosition.x;
            }

            EditorGUI.DrawRect(notes[i].rect, notes[i].color);

            // Check if the note is clicked
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && notes[i].rect.Contains(Event.current.mousePosition))
            {
                selectedNoteIndex = i; // Update the selected note index
                Repaint(); // Force repaint to reflect the selection change
                Event.current.Use(); // Consume the event to prevent other actions
            }

            notes[i].rect = GUI.Window(i, notes[i].rect, id => DrawNoteWindow(id), ""); // Pass empty string for the title
        }
        EndWindows();

        EditorGUILayout.EndScrollView();

        // Check for Ctrl + N or Ctrl + Left Click to create a new note
        if ((Event.current.control && Event.current.keyCode == KeyCode.N && Event.current.type == EventType.KeyDown) || // Ctrl + N
            (Event.current.control && Event.current.type == EventType.MouseDown && Event.current.button == 0)) // Ctrl + Left Click
        {
            CreateNewNote();
            Event.current.Use(); // Consume the event to prevent other actions
        }

        // Check for Ctrl + D to delete the last note only when there's no focused control
        if (Event.current.type == EventType.KeyDown && Event.current.control && Event.current.keyCode == KeyCode.D && GUI.GetNameOfFocusedControl() == "")
        {
            // If the user confirms, delete the last created note
            if (EditorUtility.DisplayDialog("Destroy Last Note", "Are you sure you want to destroy the last created note?", "Yes", "No"))
            {
                DeleteLastNote();
                Event.current.Use(); // Consume the event to prevent other actions
            }
        }

        // Check for Ctrl + S to save notes
        if (Event.current.type == EventType.KeyDown && Event.current.control && Event.current.keyCode == KeyCode.S)
        {
            SaveNotes();
            Event.current.Use(); // Consume the event to prevent other actions
        }
    }

    private void UpdateButtonColors(Color color)
    {
        // Update the button colors to match the selected color
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 11, // Adjust the font size as needed
        };

        // Apply the updated button style to the buttons
        GUI.skin.button = buttonStyle;
    }

    private void DeleteLastNote()
    {
        if (notes.Count > 0)
        {
            notes.RemoveAt(notes.Count - 1);
            SaveNotes();
        }
    }

    private void DrawNoteWindow(int id)
    {
        // At the beginning of your DrawNoteWindow method
        GUI.changed = false; // Explicitly reset GUI.changed state
        Note note = notes[id];

        // Draw the note's background color
        EditorGUI.DrawRect(new Rect(0, 0, note.rect.width, note.rect.height), note.color);

        GUIStyle titleInputStyle = new GUIStyle(GUI.skin.textField)
        {
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = textColor }, // Regular text color
            hover = { textColor = textColor }, // Maintain text color on hover
            active = { textColor = textColor }, // Maintain text color when active/clicked
            focused = { textColor = textColor } // Maintain text color when the field is focused
        };


        // Define a GUIStyle for bold titles.
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
            fontSize = 14,
            normal = { textColor = textColor }, // Regular text color
            hover = { textColor = textColor }, // Maintain text color on hover
            active = { textColor = textColor }, // Maintain text color when active/clicked
            focused = { textColor = textColor } // Maintain text color when the field is focused
        };

        // Adjusted GUIStyle for the delete button without bold text
        GUIStyle deleteButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold, // Make text bold
            fontSize = 11, // Adjust the font size as needed
            normal = { textColor = Color.red },
            hover = { textColor = Color.red },
            active = { textColor = Color.red },
            focused = { textColor = Color.red }
        };

        // Render the text label for the title with the bold style.
        GUI.Label(new Rect(0, 2, note.rect.width, 20), note.title, titleStyle); // Use the persistent title here

        GUILayout.BeginHorizontal();

        GUILayout.Space(1);

        // Use a temporary variable for the input field to clear after update
        if (string.IsNullOrEmpty(note.tempTitleInput))
        {
            note.tempTitleInput = ""; // Reset temporary input when empty
        }

        // Render the text field for the title with the bold style.
        string tempTitleInput = EditorGUILayout.TextField(note.tempTitleInput, titleInputStyle);

        // Check for changes in the text field.
        if (GUI.changed)
        {
            note.title = tempTitleInput; // Update the persistent title
            note.tempTitleInput = ""; // Clear the temporary input field after update
            SaveNotes(); // Save the updated title persistently
            Repaint(); // Force repaint to reflect the change
        }

        // Add a color picker and update the note's color
        note.color = EditorGUILayout.ColorField(note.color, GUILayout.Width(40));

        if (GUILayout.Button("X", deleteButtonStyle, GUILayout.Width(20), GUILayout.Height(20)))
        {
            // If the user confirms, delete the note
            if (EditorUtility.DisplayDialog("Destroy Note", $"Are you sure you want to delete this note?", "Yes", "No"))
            {
                notes.RemoveAt(id);
                SaveNotes();
            }
        }

        GUILayout.EndHorizontal();

        GUIStyle style = new GUIStyle(GUI.skin.textArea) 
        {   
            wordWrap = true, 
            normal = { textColor = textColor },
            hover = { textColor = textColor },
            active = { textColor = textColor },
            focused = { textColor = textColor }
        };
        GUIContent content = new GUIContent(note.content);
        Vector2 size = style.CalcSize(content);

        // Display the content with resizing capability
        Rect contentRect = GUILayoutUtility.GetRect(content, style, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true), GUILayout.MinHeight(size.y));
        note.content = EditorGUI.TextArea(contentRect, note.content, style);

        // Update note's size based on content
        float minHeight = Mathf.Max(size.y + EditorGUIUtility.singleLineHeight * 3, 100);
        float minWidth = Mathf.Max(size.x + 20, 200);
        note.rect.size = new Vector2(minWidth, minHeight);

        // Display the resize handle
        Rect resizeHandleRect = new Rect(note.rect.width - 10, note.rect.height - 10, 10, 10);
        GUI.Box(resizeHandleRect, GUIContent.none, GUIStyle.none);

        // Handle window dragging
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }


    private void SaveNotes()
    {
        // Save individual notes
        string notesJson = JsonUtility.ToJson(new Serialization(notes));
        EditorPrefs.SetString("NecroNotes_Data", notesJson);

        // Save the selectedColor
        string selectedColorJson = JsonUtility.ToJson(selectedColor);
        EditorPrefs.SetString("NecroNotes_SelectedColor", selectedColorJson);

        // Save the editorBackgroundColor
        string editorBgColorJson = JsonUtility.ToJson(editorBackgroundColor);
        EditorPrefs.SetString("NecroNotes_EditorBgColor", editorBgColorJson);

        // Your existing save logic...
        string textColorJson = JsonUtility.ToJson(textColor);
        EditorPrefs.SetString("NecroNotes_TextColor", textColorJson);
    }

    private void LoadNotes()
    {
        // Load individual notes
        string notesJson = EditorPrefs.GetString("NecroNotes_Data", JsonUtility.ToJson(new Serialization(new List<Note>())));
        Serialization data = JsonUtility.FromJson<Serialization>(notesJson);
        notes = data.notes;

        // Load the selectedColor
        string selectedColorJson = EditorPrefs.GetString("NecroNotes_SelectedColor", JsonUtility.ToJson(Color.green)); // Default to green if not set
        selectedColor = JsonUtility.FromJson<Color>(selectedColorJson);

        // Load the editorBackgroundColor
        string editorBgColorJson = EditorPrefs.GetString("NecroNotes_EditorBgColor", JsonUtility.ToJson(new Color(0.8f, 0.8f, 0.8f, 1.0f))); // Default color
        editorBackgroundColor = JsonUtility.FromJson<Color>(editorBgColorJson);

        string textColorJson = EditorPrefs.GetString("NecroNotes_TextColor", JsonUtility.ToJson(Color.white));
        textColor = JsonUtility.FromJson<Color>(textColorJson);
    }


    private void CreateNewNote()
    {
        // Get the current mouse position relative to the editor window
        Vector2 mousePosition = Event.current.mousePosition;

        // Calculate the center position for the new note
        float noteX = mousePosition.x - 100; // Half of the note's width (200/2)
        float noteY = mousePosition.y - 200; // Half of the note's height (100/2)

        // Spawn a new note at the calculated position
        notes.Add(new Note(noteX, noteY, 200, 100, "", new Color(selectedColor.r, selectedColor.g, selectedColor.b, 1f))); // Ensure alpha is set to 1f
        SaveNotes();
    }


    private void ImportNotes()
    {
        string filePath = EditorUtility.OpenFilePanel("Import Notes", "", "json,txt");
        if (!string.IsNullOrEmpty(filePath))
        {
            string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (fileExtension)
            {
                case ".json":
                    ImportNotesFromJson(filePath);
                    break;
                case ".txt":
                    ImportNotesFromText(filePath);
                    break;
                default:
                    Debug.LogError("Unsupported file format.");
                    break;
            }
        }
    }

    private void ImportNotesFromJson(string filePath)
    {
        string json = File.ReadAllText(filePath);
        Serialization data = JsonUtility.FromJson<Serialization>(json);
        notes = data.notes;
        Debug.Log("Notes imported successfully from JSON.");
    }

    private void ImportNotesFromText(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        notes.Clear(); // Clear existing notes before importing new ones

        Note currentNote = null; // Start with no current note
        Color defaultColor = new Color(0.3f, 0.3f, 0.3f); // Darker grey color

        foreach (string line in lines)
        {
            if (line.StartsWith("Title: "))
            {
                // Check if we're already processing a note
                if (currentNote != null)
                {
                    notes.Add(currentNote); // Add the completed note to the list
                }
                // Start a new note
                currentNote = new Note();
                currentNote.title = line.Substring("Title: ".Length);
                currentNote.color = defaultColor; // Ensure the default color is set
            }
            else if (line.StartsWith("Content: ") && currentNote != null)
            {
                currentNote.content = line.Substring("Content: ".Length);
            }
            // Add other properties here as needed
        }

        // Add the last processed note if it exists and has a title
        if (currentNote != null && !string.IsNullOrEmpty(currentNote.title))
        {
            notes.Add(currentNote);
        }

        Debug.Log("Notes imported successfully from Text.");
    }

    private void SaveEditorBackgroundColor()
    {
        string colorString = JsonUtility.ToJson(editorBackgroundColor);
        EditorPrefs.SetString("NecroNotes_EditorBgColor", colorString);
    }

    private void LoadEditorBackgroundColor()
    {
        string colorString = EditorPrefs.GetString("NecroNotes_EditorBgColor", JsonUtility.ToJson(new Color(0.8f, 0.8f, 0.8f, 1.0f)));
        editorBackgroundColor = JsonUtility.FromJson<Color>(colorString);
    }

    private void ExportNotes()
    {
        int option = EditorUtility.DisplayDialogComplex(
            "Export Notes",
            "Choose the format to export your notes:",
            "JSON",
            "Text",
            "Cancel");

        switch (option)
        {
            case 0: // JSON
                ExportNotesAsJson();
                break;
            case 1: // Text
                ExportNotesAsText();
                break;
        }
    }

    private void ExportNotesAsJson()
    {
        string json = JsonUtility.ToJson(new Serialization(notes));

        // Open a file save dialog to choose where to save the exported notes
        string filePath = EditorUtility.SaveFilePanel("Export Notes", "", "notes.json", "json");

        if (!string.IsNullOrEmpty(filePath))
        {
            File.WriteAllText(filePath, json);
            Debug.Log("Notes exported successfully to: " + filePath);
        }
    }

    private void ExportNotesAsText()
    {
        string filePath = EditorUtility.SaveFilePanel("Export Notes as Text", "", "notes.txt", "txt");
        if (!string.IsNullOrEmpty(filePath))
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var note in notes)
                {
                    writer.WriteLine($"Title: {note.title}");
                    writer.WriteLine($"Content: {note.content}");
                    // Omitting the color information from the export
                    writer.WriteLine("-----------\n");
                }
            }
            Debug.Log("Notes exported successfully to: " + filePath);
        }
    }

    private void ToggleColorPicker(bool show)
    {
        showColorPicker = show;
    }

    private float GetDynamicTopAreaHeight()
    {
        float baseHeight; // Height for buttons and color pickers
        float logoHeight; // Additional height based on the logo size

        switch (currentLogoSize)
        {
            case LogoSize.Small:
                logoHeight = -28; // Example height + padding for Small
                ToggleColorPicker(false);
                break;
            case LogoSize.Medium:
                logoHeight = 62; // Example height + padding for Medium
                ToggleColorPicker(true);
                break;
            case LogoSize.Large:
            default:
                logoHeight = 92; // Example height + padding for Large
                ToggleColorPicker(true);
                break;
        }

        baseHeight = 155; // Assuming this is enough for buttons/color pickers below the logo

        return logoHeight + baseHeight; // Total dynamic height
    }


    [System.Serializable]
    private class Serialization
    {
        public List<Note> notes;

        public Serialization(List<Note> notes)
        {
            this.notes = notes;
        }
    }
}