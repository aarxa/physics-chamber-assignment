using UnityEngine;

// AI assistance: Codex generated this small IMGUI status panel and restart button.
public class ChamberHUD : MonoBehaviour
{
    [SerializeField] private ChamberRun run;
    private GUIStyle _title;
    private GUIStyle _body;
    private GUIStyle _status;

    private void OnGUI()
    {
        if (run == null)
            return;

        if (_title == null)
        {
            _title = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
            _title.normal.textColor = Color.white;
            _body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            _body.normal.textColor = new Color(0.8f, 0.87f, 0.94f);
            _status = new GUIStyle(_body) { fontSize = 19, fontStyle = FontStyle.Bold };
        }

        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        Color previousColor = GUI.color;
        GUI.color = new Color(0.035f, 0.055f, 0.09f, 0.94f);
        GUI.DrawTexture(new Rect(18, 18, 292, 362), Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUI.Label(new Rect(34, 32, 270, 38), "PHYSICS CHAMBER", _title);
        GUI.Label(new Rect(34, 77, 250, 38), $"TIME   {run.ElapsedSeconds:000.00} s", _status);
        _status.normal.textColor = run.HasSucceeded ? new Color(0.3f, 1f, 0.6f) :
            run.IsUnlocked ? new Color(0.35f, 0.85f, 1f) : new Color(1f, 0.78f, 0.3f);
        string status = run.HasSucceeded ? "DEPOSIT COMPLETE" : run.IsUnlocked ? "DOOR OPEN" : "DOOR LOCKED";
        GUI.Label(new Rect(34, 116, 250, 36), status, _status);
        GUI.Label(new Rect(34, 158, 255, 82), run.HasSucceeded
            ? "Success! The orange Payload entered the DepositZone after the door opened."
            : run.IsUnlocked
                ? "Push the orange Payload through the open doorway into the green DepositZone."
                : "Move the blue Actor onto the yellow sensor to open the red door.", _body);
        GUI.Label(new Rect(34, 247, 255, 65), "WASD  Move / push\nR  Restart if the crate gets stuck", _body);
        if (GUI.Button(new Rect(34, 325, 250, 36), "Restart chamber (R)"))
            run.Restart();
        GUI.matrix = previousMatrix;
    }
}
