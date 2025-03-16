using UnityEditor;
using UnityEngine;

namespace NairobiHustle.Payment
{
#if UNITY_EDITOR
    [CustomEditor(typeof(MPesaService))]
    public class MPesaServiceEditor : Editor
    {
        private SerializedProperty merchantIdProp;
        private SerializedProperty apiKeyProp;
        private SerializedProperty shortCodeProp;
        private SerializedProperty useSandboxProp;

        private void OnEnable()
        {
            merchantIdProp = serializedObject.FindProperty("merchantId");
            apiKeyProp = serializedObject.FindProperty("apiKey");
            shortCodeProp = serializedObject.FindProperty("shortCode");
            useSandboxProp = serializedObject.FindProperty("useSandbox");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("M-Pesa Configuration - Add your official credentials from the Safaricom Developer Portal", MessageType.Info);
            EditorGUILayout.PropertyField(merchantIdProp, new GUIContent("Merchant ID"));
            EditorGUILayout.PropertyField(apiKeyProp, new GUIContent("API Key"));
            EditorGUILayout.PropertyField(shortCodeProp, new GUIContent("Short Code"));
            EditorGUILayout.PropertyField(useSandboxProp, new GUIContent("Use Sandbox"));

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Transaction Settings", MessageType.None);
            var service = (MPesaService)target;
            var minimumProp = serializedObject.FindProperty("minimumAmount");
            var maximumProp = serializedObject.FindProperty("maximumAmount");
            var timeoutProp = serializedObject.FindProperty("transactionTimeout");

            EditorGUILayout.PropertyField(minimumProp);
            EditorGUILayout.PropertyField(maximumProp);
            EditorGUILayout.PropertyField(timeoutProp);

            if (minimumProp.floatValue > maximumProp.floatValue) {
                EditorGUILayout.HelpBox("Minimum amount cannot exceed maximum amount!", MessageType.Error);
            }

            if (minimumProp.floatValue < 0 || maximumProp.floatValue < 0) {
                EditorGUILayout.HelpBox("Amounts cannot be negative!", MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Test Valid Transaction (500 KES)")) {
                service.ProcessPayment(500);
            }
            
            if (GUILayout.Button("Test Invalid Transaction (-100 KES)")) {
                service.ProcessPayment(-100);
            }
            
            if (GUILayout.Button("View Transaction History")) {
                service.LoadTransactionHistory();
                Debug.Log($"Last transaction: {JsonUtility.ToJson(service.GetLastTransaction())}");
            }
            
            if (GUILayout.Button("Clear Test Data")) {
                PlayerPrefs.DeleteKey("MPesaTransactions");
                PlayerPrefs.DeleteKey("PlayerBalance");
                service.LoadTransactionHistory();
            }
        }
    }
#endif
}