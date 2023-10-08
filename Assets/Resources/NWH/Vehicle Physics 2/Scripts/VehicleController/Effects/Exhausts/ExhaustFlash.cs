using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Effects
{
    /// <summary>
    ///     Controls exhaust flashes. These are achieved through changing the color of the flame textures.
    /// </summary>
    [Serializable]
    public partial class ExhaustFlash : Effect
    {
        public bool flash;

        public List<Light> flashLights = new List<Light>();

        public float flashChance = 0.2f;

        public bool flashOnRevLimiter = true;

        public bool flashOnShift = true;

        public float flashDuration = 0.05f;

        /// <summary>
        ///     Textures representing exhaust flash. If multiple are assigned a random texture will be chosen for each flash.
        /// </summary>
        [Tooltip(
            "Textures representing exhaust flash. If multiple are assigned a random texture will be chosen for each flash.")]
        public List<Texture2D> flashTextures = new List<Texture2D>();


        /// <summary>
        ///     Mesh renderer(s) for the exhaust flash meshes. Materials used should have '_TintColor' property.
        /// </summary>
        [Tooltip(
            "    Mesh renderer(s) for the exhaust flash meshes. Materials used should have '_TintColor' property.")]
        public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();


        public UnityEvent onFlash = new UnityEvent();


        public override void Initialize()
        {
            base.Initialize();

            vc.powertrain.engine.onRevLimiter.AddListener(FlashWithChance);
        }


        public override void SetDefaults(VehicleController vc)
        {
            flashLights = new List<Light>();
            flashTextures = new List<Texture2D>();
            meshRenderers = new List<MeshRenderer>();
        }

        public void Flash()
        {
            Flash(true);
        }


        public void Flash(bool triggerEvent)
        {
            vc.StartCoroutine(FlashCoroutine(triggerEvent));
        }


        public void FlashWithChance()
        {
            FlashWithChance(true, flashChance);
        }

        public void FlashWithChance(bool triggerEvent, float chance)
        {
            if (Random.Range(0f, 1f) < chance)
            {
                vc.StartCoroutine(FlashCoroutine(triggerEvent));
            }
        }


        private IEnumerator FlashCoroutine(bool triggerEvent)
        {
            int textureCount = flashTextures.Count;
            foreach (MeshRenderer renderer in meshRenderers)
            {
                renderer.material.SetTexture("_MainTex", flashTextures[Random.Range(0, textureCount)]);
                float r = Random.Range(0.2f, 0.6f);
                renderer.transform.localScale = new Vector3(r, r, r);
                renderer.enabled = true;
            }

            foreach (Light light in flashLights)
            {
                light.enabled = true;
            }

            if (triggerEvent)
            {
                onFlash.Invoke();
            }

            yield return new WaitForSeconds(Random.Range(flashDuration * 0.5f, flashDuration * 1.5f));

            foreach (MeshRenderer renderer in meshRenderers)
            {
                renderer.enabled = false;
            }

            foreach (Light light in flashLights)
            {
                light.enabled = false;
            }

            yield return null;
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Effects
{
    [CustomPropertyDrawer(typeof(ExhaustFlash))]
    public partial class ExhaustFlashDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("flashOnRevLimiter");
            drawer.Field("flashOnShift");
            drawer.Field("flashChance");
            drawer.ReorderableList("meshRenderers");
            drawer.ReorderableList("flashTextures");
            drawer.ReorderableList("flashLights");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
