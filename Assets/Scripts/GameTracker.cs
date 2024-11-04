using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class GameTracker : MonoBehaviour
{
    public static GameTracker current;
    public JSONWriter<UserData> jsonWriter;

    [SerializeField] private Transform m_player;
    [SerializeField] private PlayerMovement m_playerMovement;
    [SerializeField] private NoiseMap m_terrainGenerator;

    [SerializeField] private UserData m_session;
    
    [SerializeField] private float m_startTime;
    [SerializeField] private bool m_isWriting = false;
    [SerializeField] private float m_writeDelay = 0.25f;
    private IEnumerator m_updateCoroutine = null;

    [DllImport("__Internal")]
    public static extern void SyncFS();

    private void Awake() {
        current = this;
        string pathToSaveData = System.IO.Path.Combine(Application.persistentDataPath,"data.json");
        jsonWriter = new JSONWriter<UserData>(pathToSaveData);
    }

    public void StartTracking() {
        if (m_isWriting) {
            Debug.LogError("Session is currently writing - no need to start tracking.");
            return;
        }
        m_updateCoroutine = UpdateTracking();
        StartCoroutine(m_updateCoroutine);
    }

    private IEnumerator UpdateTracking() {
        m_session = new UserData(SessionMemory.current.seed, SessionManager.current.playerStartPosition, SessionManager.current.playerEndPosition);
        m_startTime = Time.time;
        float lastTimestamp = 0f;
        m_isWriting = true;
        WaitForSeconds waitSecs = new WaitForSeconds(m_writeDelay);

        while(m_isWriting) {
            // Each row must contain the timestamp, user's position, forward, noisemap position, and whether the map is being held at the current moment
            float timestamp = Time.time - m_startTime;
            float deltaTime = timestamp - lastTimestamp;
            Vector3 pos = m_player.transform.position;
            Vector3 forward = m_player.transform.forward;
            Vector3 noiseMapPos = new Vector3(pos.x, m_terrainGenerator.QueryHeightAtWorldPos(pos.x, pos.z, out int coordX, out int coordY), pos.z);
            int isHoldingMap = SessionManager.current.isShowingMap ? 1 : 0;
            float stamina = m_playerMovement.GetFlightStamina();
            
            m_session.AddRow(new UserRow(timestamp, pos, forward, noiseMapPos, isHoldingMap, stamina, deltaTime));
            lastTimestamp = timestamp;
            yield return waitSecs;
        }
    }

    public void StopTracking() {
        m_isWriting = false;
    }

    public string GetSerializedJSON() {
        if (m_session == null) {
            Debug.LogError("Cannot get serialized user session due to session not being initialized");
            return null;
        }

        return jsonWriter.SerializeData(m_session, false);
    }

    public void CopyToClipboard() {
        if (m_session == null) {
            Debug.LogError("Cannot get serialized user session due to session not being initialized");
            return;
        }

        string serializedString = jsonWriter.SerializeData(m_session, false);
        TextEditor te = new TextEditor();
        te.text = serializedString;
        te.SelectAll();
        te.Copy();
    }

    public void SaveUserData() {
        if (m_session == null) {
            Debug.LogError("Cannot get serialized user session due to session not being initialized");
            return;
        }
        string serializedString = jsonWriter.SerializeData(m_session, false);
        DateTime dt = DateTime.Now;
        string dts = dt.ToString("yyyy_MM_dd-THH_mm_ss");
        string saveFilename = $"{m_session.seed.ToString()}-{dts}.json";
        WebGLFileSaver.SaveFile(serializedString, saveFilename, "application/json");
    }

    public UserData Load() {
        return jsonWriter.DeserializeData();
    }

    public void Save(UserData data) {
        jsonWriter.SerializeData(data);

        #if UNITY_WEBGL && !UNITY_EDITOR
            SyncFS();
        #endif
    }

    private void OnDestroy() {
        if (m_updateCoroutine != null) StopCoroutine(m_updateCoroutine);
    }
}

[System.Serializable]
public class JSONWriter<JsonData> {
    public string filepath;
    public JSONWriter(string path) {
        this.filepath = path;
    }

    public string SerializeData(JsonData data, bool writeToFile = true) {
        string jsonDataString = JsonUtility.ToJson(data, true);
        if (writeToFile) File.WriteAllText(this.filepath, jsonDataString);
        return jsonDataString;
    }

    public JsonData DeserializeData() {
        if (File.Exists(this.filepath)) {
            string loadedJsonDataString = File.ReadAllText(this.filepath);
            return JsonUtility.FromJson<JsonData>(loadedJsonDataString);
        }
        return default(JsonData);
    }
}

[System.Serializable]
public class UserData {
    public string seed;
    public Vector3 startPosition;
    public Vector3 endPosition;
    public List<UserRow> positions;
    public UserData(string seed, Vector3 start, Vector3 end) {
        this.seed = seed;
        this.startPosition = start;
        this.endPosition = end;
        this.positions = new List<UserRow>();
    }
    public void AddRow(UserRow row) {
        this.positions.Add(row);
    }
}

[System.Serializable]
public class UserRow {
    public float timestamp;
    public Vector3 position;
    public Vector3 forward;
    public Vector3 noisemapPosition;
    public int mapHeld;
    public float stamina;
    public float deltaTime;
    public UserRow(float timestamp, Vector3 position, Vector3 forward, Vector3 noisemapPosition, int mapHeld, float stamina, float deltaTime) {
        this.timestamp = timestamp;
        this.position = position;
        this.forward = forward;
        this.noisemapPosition = noisemapPosition;
        this.mapHeld = mapHeld;
        this.stamina = stamina;
        this.deltaTime = deltaTime;
    }
}
