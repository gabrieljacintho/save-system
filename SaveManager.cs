using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SaveSystem
{
    public static class SaveManager
    {
        private static Dictionary<string, object> _saveDictionary = new Dictionary<string, object>();
        private static string FilePath => $"{Application.persistentDataPath}/{Application.identifier}.gameData";

        public static Action OnReset;
        public static Action OnLoad;
        public static Action OnSave;
        
        #region Getters
        
        public static float GetFloat(string key, float defaultValue = 0f)
        {
            float value = defaultValue;
            if (HasSaveKey(key))
            {
                value = Convert.ToSingle(GetValue(key));
            }
            else if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetFloat(key);
                SetFloat(key, value);
            }

            return value;
        }
        
        public static int GetInt(string key, int defaultValue = 0)
        {
            int value = defaultValue;
            if (HasSaveKey(key))
            {
                value = Convert.ToInt32(GetValue(key));
            }
            else if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetInt(key);
                SetInt(key, value);
            }

            return value;
        }

        public static long GetLong(string key, long defaultValue = 0)
        {
            long value = defaultValue;
            if (HasSaveKey(key))
            {
                value = Convert.ToInt64(GetValue(key));
            }
            else if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefsX.GetLong(key);
                SetLong(key, value);
            }

            return value;
        }

        public static string GetString(string key, string defaultValue = null)
        {
            string value = defaultValue;
            if (HasSaveKey(key))
            {
                value = (string)GetValue(key);
            }
            else if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetString(key);
                SetString(key, value);
            }

            return value;
        }
        
        public static string[] GetStringArray(string key, string[] defaultValue = null)
        {
            string[] values = defaultValue;
            if (HasSaveKey(key))
            {
                string value = GetString(key);
                int separatorIndex = value.IndexOf("|"[0]);
                if (separatorIndex < 4) {
                    Debug.LogError ("Corrupt preference file for " + key);
                    return new String[0];
                }
                
                byte[] bytes = Convert.FromBase64String(value.Substring(0, separatorIndex));
                
                int numberOfEntries = bytes.Length - 1;
                values = new string[numberOfEntries];
                
                int valueIndex = separatorIndex + 1;
                for (var i = 0; i < numberOfEntries; i++)
                {
                    int valueLength = bytes[i++];
                    values[i] = value.Substring(valueIndex, valueLength);
                    valueIndex += valueLength;
                }
            }
            else if (PlayerPrefs.HasKey(key))
            {
                values = PlayerPrefsX.GetStringArray(key);
                SetStringArray(key, values);
            }

            return values;
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            bool value = defaultValue;
            if (HasSaveKey(key))
            {
                value = (int)Convert.ToSingle(GetValue(key)) == 1;
            }
            else if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefsX.GetBool(key);
                SetBool(key, value);
            }

            return value;
        }
        
        private static object GetValue(string key, object defaultValue = default)
        {
            return HasAnySaveKey(key) ? _saveDictionary[key] : defaultValue;
        }
        
        #endregion
        
        #region Setters
        
        public static void SetFloat(string key, float value)
        {
            SetValue(key, value);
        }
        
        public static void SetInt(string key, int value)
        {
            SetValue(key, value);
        }

        public static void SetLong(string key, long value)
        {
            SetValue(key, value);
        }
        
        public static void SetString(string key, string value)
        {
            SetValue(key, value);
        }
        
        public static bool SetStringArray(string key, string[] values)
        {
            byte[] bytes = new byte[values.Length];
 
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                {
                    Debug.LogError("Cannot save null entries in the string array! (key: \"" + key + "\")");
                    return false;
                }
                
                if (values[i].Length > 255)
                {
                    Debug.LogError("Strings cannot be longer than 255 characters! (key: \"" + key + "\")");
                    return false;
                }
                
                bytes[i] = (byte)values[i].Length;
            }

            string value = Convert.ToBase64String(bytes) + "|" + string.Join("", values);
            SetValue(key, value);
            
            return true;
        }
        
        public static void SetBool(string key, bool value)
        {
            SetValue(key, value ? 1 : 0);
        }
        
        private static void SetValue(string key, object value)
        {
            if (!_saveDictionary.ContainsKey(key))
            {
                _saveDictionary.Add(key, value);
            }
            else
            {
                _saveDictionary[key] = value;
            }
            
            Save();
        }
        
        #endregion

        public static string GetSaveJson()
        {
            return JsonConvert.SerializeObject(_saveDictionary);
        }

        public static void SetSaveFromJson(string value)
        {
            var resultDic = JsonConvert.DeserializeObject<Dictionary<string, object>>(value);
            _saveDictionary = resultDic;
        }
        
        public static void Save()
        {
            try
            {
                using FileStream fileStream = new FileStream(FilePath, FileMode.Create);
                using StreamWriter streamWriter = new StreamWriter(fileStream);
                
                string json = GetSaveJson();
                streamWriter.Write(json);
            }
            catch
            {
                SaveNewFile();
            }
            
            OnSave?.Invoke();
        }

        private static void SaveNewFile()
        {
            DeleteSave();

            try
            {
                using FileStream fileStream = new FileStream(FilePath, FileMode.CreateNew);
                using StreamWriter streamWriter = new StreamWriter(fileStream);

                string json = JsonConvert.SerializeObject(_saveDictionary);
                streamWriter.Write(json);
            }
            catch (FileNotFoundException e)
            {
                Debug.LogError($"The save file was not found: '{e}'");
            }
            catch (DirectoryNotFoundException e)
            {
                Debug.LogError($"The save directory was not found: '{e}'");
            }
            catch (IOException e)
            {
                Debug.LogError($"The save file could not be opened: '{e}'");
            }
            catch (Exception e)
            {
                Debug.LogError($"The save file could not be created: '{e}'");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Load()
        {
            if (!File.Exists(FilePath))
            {
                return;
            }

            try
            {
                using FileStream fileStream = new FileStream(FilePath, FileMode.Open);
                using StreamReader streamReader = new StreamReader(fileStream);
                
                string json = streamReader.ReadToEnd();
                _saveDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }
            catch (FileNotFoundException e)
            {
                Debug.LogError($"The save file was not found: '{e}'");
                _saveDictionary = new Dictionary<string, object>();
            }
            catch (DirectoryNotFoundException e)
            {
                Debug.LogError($"The save directory was not found: '{e}'");
                _saveDictionary = new Dictionary<string, object>();
            }
            catch (IOException e)
            {
                Debug.LogError($"The save file could not be opened: '{e}'");
                _saveDictionary = new Dictionary<string, object>();
            }
            catch (Exception e)
            {
                Debug.LogError($"The save file could not be loaded: '{e}'");
                _saveDictionary = new Dictionary<string, object>();
            }
            
            OnLoad?.Invoke();
        }

        public static void Reset()
        {
            _saveDictionary = new Dictionary<string, object>();
            PlayerPrefs.DeleteAll();
            OnReset?.Invoke();
            var allResettable = Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveReset>();
            foreach (var resettable in allResettable)
            {
                resettable.ResetSave();
            }
            
            Save();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Clear All Saves")]
#endif
        public static void DeleteAll()
        {
            DeleteSave();
            PlayerPrefs.DeleteAll();
            
            Reset();
        }

        private static void DeleteSave()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }

        public static void DeleteKey(string key)
        {
            if (_saveDictionary.ContainsKey(key))
            {
                _saveDictionary.Remove(key);
            }
            
            PlayerPrefs.DeleteKey(key);
        }
        
        /// <summary>
        /// Check if save key exist, include PlayerPrefs one.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool HasAnySaveKey(string key)
        {
            return HasSaveKey(key) || PlayerPrefs.HasKey(key);
        }

        /// <summary>
        /// Check if save manager game file contains the target save key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool HasSaveKey(string key)
        {
            return _saveDictionary.ContainsKey(key);
        }
    }
}