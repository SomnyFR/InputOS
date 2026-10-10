using InputOS.Models;

namespace InputOS.Services;

public static class ControllerButtonCatalogService
{
    // ============================================================================
    // FONCTIONNALITÉ : COMMANDES PAR MODÈLE DE MANETTE
    // ============================================================================

    public static string GetFamily(string model) => model switch
    {
        "DualShock 4" or "DualSense" or "DualSense Edge" => "PlayStation",
        "Nintendo Switch Joy-Con" => "JoyCon",
        "Nintendo Switch Pro" => "Nintendo",
        _ when model.StartsWith("Xbox", StringComparison.Ordinal) => "Xbox",
        _ => "Generic"
    };

    public static ControllerButtonDefinition[] GetButtons(string model)
    {
        string family = GetFamily(model);
        bool ps = family == "PlayStation";
        bool nintendo = family is "Nintendo" or "JoyCon";
        bool joyCon = family == "JoyCon";
        List<ControllerButtonDefinition> buttons = new();

        void Add(string id, string name, string label, double x, double y,
            bool trigger = false, bool direction = false) =>
            buttons.Add(new(id, name, label, x, y, trigger, direction));

        Add("FaceBottom", ps ? "Croix" : nintendo ? "B" : "A", ps ? "×" : nintendo ? "B" : "A", 365, 165);
        Add("FaceRight", ps ? "Cercle" : nintendo ? "A" : "B", ps ? "○" : nintendo ? "A" : "B", 395, 135);
        Add("FaceLeft", ps ? "Carré" : nintendo ? "Y" : "X", ps ? "□" : nintendo ? "Y" : "X", 335, 135);
        Add("FaceTop", ps ? "Triangle" : nintendo ? "X" : "Y", ps ? "△" : nintendo ? "X" : "Y", 365, 105);

        double dpadX = ps || joyCon ? 115 : 160;
        double dpadY = ps ? 135 : 215;
        Add("DpadUp", "Croix directionnelle : haut", "↑", dpadX, dpadY - 23, direction: true);
        Add("DpadDown", "Croix directionnelle : bas", "↓", dpadX, dpadY + 23, direction: true);
        Add("DpadLeft", "Croix directionnelle : gauche", "←", dpadX - 23, dpadY, direction: true);
        Add("DpadRight", "Croix directionnelle : droite", "→", dpadX + 23, dpadY, direction: true);

        Add("LeftShoulder", ps ? "L1" : nintendo ? "L" : "LB", ps ? "L1" : nintendo ? "L" : "LB", 115, 57);
        Add("RightShoulder", ps ? "R1" : nintendo ? "R" : "RB", ps ? "R1" : nintendo ? "R" : "RB", 365, 57);
        Add("LeftTrigger", ps ? "L2" : nintendo ? "ZL" : "LT", ps ? "L2" : nintendo ? "ZL" : "LT", 115, 22, trigger: true);
        Add("RightTrigger", ps ? "R2" : nintendo ? "ZR" : "RT", ps ? "R2" : nintendo ? "ZR" : "RT", 365, 22, trigger: true);
        Add("LeftStick", "L3 — clic du joystick gauche", "L3", ps ? 165 : 115, ps ? 215 : 135);
        Add("RightStick", "R3 — clic du joystick droit", "R3", joyCon ? 365 : 300, 215);
        Add("MenuLeft", ps ? model == "DualShock 4" ? "Share" : "Create" : nintendo ? "Moins (−)" : model == "Xbox 360" ? "Back" : "View", ps ? "S" : nintendo ? "−" : "V", joyCon ? 145 : 205, joyCon ? 90 : 145);
        Add("MenuRight", ps ? "Options" : nintendo ? "Plus (+)" : model == "Xbox 360" ? "Start" : "Menu", ps ? "O" : nintendo ? "+" : "M", joyCon ? 335 : 275, joyCon ? 90 : 145);
        Add("Home", ps ? "PS" : nintendo ? "Home" : "Xbox / Guide", ps ? "PS" : nintendo ? "H" : "X", joyCon ? 380 : 240, joyCon ? 275 : family == "Xbox" ? 100 : 190);

        if (ps)
        {
            Add("Touchpad", "Appui sur le pavé tactile", "Touch", 240, 100);
            if (model != "DualShock 4") Add("Microphone", "Bouton microphone", "Mic", 240, 225);
            if (model == "DualSense Edge")
            {
                Add("FnLeft", "Fn gauche", "FnG", 175, 260);
                Add("FnRight", "Fn droit", "FnD", 305, 260);
                Add("BackLeft", "Bouton arrière gauche", "AR G", 205, 295);
                Add("BackRight", "Bouton arrière droit", "AR D", 275, 295);
            }
        }
        if (model == "Xbox Series S/X" || nintendo)
            Add("Capture", nintendo ? "Capture" : "Share", "C", joyCon ? 100 : 240, joyCon ? 275 : family == "Xbox" ? 180 : 145);
        if (model is "Xbox Elite" or "Xbox Elite Series 2")
            for (int i = 0; i < 4; i++) Add($"Paddle{i + 1}", $"Palette arrière P{i + 1}", $"P{i + 1}", 180 + i * 40, 285);
        if (joyCon)
        {
            Add("SLLeft", "SL gauche", "SLG", 68, 155);
            Add("SRLeft", "SR gauche", "SRG", 68, 195);
            Add("SLRight", "SL droit", "SLD", 412, 155);
            Add("SRRight", "SR droit", "SRD", 412, 195);
        }
        return buttons.ToArray();
    }

    // ============================================================================
    // FIN FONCTIONNALITÉ : COMMANDES PAR MODÈLE DE MANETTE
    // ============================================================================
}
