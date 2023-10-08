using System;
using System.Collections.Generic;
using NWH.Common.Utility;
using UnityEngine;
using NWH.VehiclePhysics2.Powertrain;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public partial class PowertrainComponent
    {
        /// <summary>
        ///     Angular velocity of the component.
        /// </summary>
        [Tooltip("    Angular velocity of the component.")]
        protected float _angularVelocity;

        [ShowInTelemetry]
        public float AngularVelocity
        {
            get => _angularVelocity;
        }

        protected float _torque;

        [ShowInTelemetry]
        public float Torque
        {
            get => _torque;
        }

        /// <summary>
        ///     Angular inertia of the component. Higher inertia value will result in a powertrain that is slower to spin up, but
        ///     also slower to spin down. Too high values will result in (apparent) sluggish response while too low values will
        ///     result in vehicle being easy to stall.
        /// </summary>
        [Range(0.0002f, 3f)]
        [Tooltip(
            "Angular inertia of the component. Higher inertia value will result in a powertrain that is slower to spin up, but\r\nalso slower to spin down. Too high values will result in (apparent) sluggish response while too low values will\r\nresult in vehicle being easy to stall.")]
        public float inertia = 0.02f;

        /// <summary>
        ///     Input component. Set automatically.
        /// </summary>
        [Tooltip("    Input component. Set automatically.")]
        [NonSerialized]
        public PowertrainComponent input;

        /// <summary>
        ///     Name of the component. Only unique names should be used on the same vehicle.
        /// </summary>
        [Tooltip("    Name of the component. Only unique names should be used on the same vehicle.")]
        public string name;

        /// <summary>
        ///     Output component.
        /// </summary>
        [Tooltip("    Output component.")]
        public PowertrainComponent outputA;

        public VehicleController vc;

        [NonSerialized] public bool componentInputIsNull;
        [NonSerialized] protected float _lowerAngularVelocityLimit = -Mathf.Infinity;
        [NonSerialized] protected bool _outputAIsNull;
        [NonSerialized] protected float _upperAngularVelocityLimit = Mathf.Infinity;

        [SerializeField]
        protected OutputSelector outputASelector = new OutputSelector();

        /// <summary>
        ///     Damage in range of 0 to 1 that the component has received.
        /// </summary>
        [NonSerialized] private float _componentDamage;


        public PowertrainComponent()
        {
        }


        public PowertrainComponent(float inertia, string name)
        {
            this.name = name;
            this.inertia = inertia;
        }


        /// <summary>
        ///     Returns current component damage.
        /// </summary>
        public float ComponentDamage
        {
            get { return _componentDamage; }
            set { _componentDamage = value > 1 ? 1 : value < 0 ? 0 : value; }
        }

        /// <summary>
        ///     Minimum angular velocity a component can physically achieve.
        /// </summary>
        public float LowerAngularVelocityLimit
        {
            get { return _lowerAngularVelocityLimit; }
            set { _lowerAngularVelocityLimit = value; }
        }

        /// <summary>
        ///     RPM of component.
        /// </summary>
        public float RPM
        {
            get { return UnitConverter.AngularVelocityToRPM(_angularVelocity); }
        }

        /// <summary>
        ///     Maximum angular velocity a component can physically achieve.
        /// </summary>
        public float UpperAngularVelocityLimit
        {
            get { return _upperAngularVelocityLimit; }
            set { _upperAngularVelocityLimit = value; }
        }


        /// <summary>
        ///     Initializes PowertrainComponent.
        /// </summary>
        public virtual void Initialize(VehicleController vc)
        {
            if (inertia < 0.0001f)
            {
                inertia = 0.0001f;
            }

            this.vc = vc;
            componentInputIsNull = input == null;
            _outputAIsNull = outputA == null;
        }


        /// <summary>
        ///     Gets called before solver.
        /// </summary>
        public virtual void OnPrePhysicsStep(float dt)
        {
        }


        /// <summary>
        ///     Gets called after solver has finished.
        /// </summary>
        public virtual void OnPostPhysicsStep(float dt)
        {
            _angularVelocity = _angularVelocity < _lowerAngularVelocityLimit
                                  ? _lowerAngularVelocityLimit
                                  : _angularVelocity;
            _angularVelocity = _angularVelocity > _upperAngularVelocityLimit
                                  ? _upperAngularVelocityLimit
                                  : _angularVelocity;
        }


        public virtual void OnEnable()
        {
        }


        public virtual void OnDisable()
        {
        }


        public virtual void SetDefaults(VehicleController vc)
        {
            this.vc = vc;

            inertia = 0.02f;
            outputASelector = new OutputSelector();
        }


        public virtual void Validate(VehicleController vc)
        {
            Debug.Log($"|---Validating {GetType().Name}");

            if (inertia < 0.0001f)
            {
                inertia = 0.0001f;
                Debug.LogWarning($"{name}: Inertia must be larger than 0.002f. Setting to 0.002f.");
            }
        }


        /// <summary>
        ///     Finds which powertrain component has its output set to this component.
        /// </summary>
        public virtual void FindInput(Powertrain powertrain)
        {
            List<PowertrainComponent> outputs = new List<PowertrainComponent>();
            foreach (PowertrainComponent component in powertrain.Components)
            {
                component.GetAllOutputs(ref outputs);
                foreach (PowertrainComponent output in outputs)
                {
                    if (output != null && output == this)
                    {
                        input = component;
                        componentInputIsNull = false;
                        return;
                    }
                }
            }

            input = null;
            componentInputIsNull = true;
        }


        /// <summary>
        ///     Retrieves and sets output powertrain components.
        /// </summary>
        /// <param name="powertrain"></param>
        public virtual void FindOutputs(Powertrain powertrain)
        {
            if (string.IsNullOrEmpty(outputASelector.name))
            {
                return;
            }

            PowertrainComponent output = powertrain.GetComponent(outputASelector.name);
            if (output == null)
            {
                Debug.LogError($"Unknown component '{outputASelector.name}' on '{name}'");
                return;
            }

            outputA = output;
        }


        /// <summary>
        ///     Retruns a list of PowertrainComponents that this component outputs to.
        /// </summary>
        public virtual void GetAllOutputs(ref List<PowertrainComponent> outputs)
        {
            outputs.Clear();
            outputs.Add(outputA);
        }


        public virtual float QueryAngularVelocity(float inputAngularVelocity, float dt)
        {
            _angularVelocity = inputAngularVelocity;
            if (_outputAIsNull)
            {
                return 0;
            }

            float Wa = outputA.QueryAngularVelocity(inputAngularVelocity, dt);
            return Wa;
        }


        public virtual float QueryInertia()
        {
            if (_outputAIsNull)
            {
                return inertia;
            }

            float Ii = inertia;
            float Ia = outputA.QueryInertia();
            float I = Ii + Ia;
            return I;
        }


        public virtual float ForwardStep(float torque, float inertiaSum, float dt)
        {
            _torque = torque;

            if (_outputAIsNull)
            {
                return torque;
            }

            return outputA.ForwardStep(torque, inertiaSum + inertia, dt);
        }


        public void SetOutput(PowertrainComponent outputComponent)
        {
            if (string.IsNullOrEmpty(outputComponent.name))
            {
                Debug.LogWarning("Trying to set powertrain component output to a nameless component. " +
                                 "Output will be set to [none]");
            }

            SetOutput(outputComponent.name);
        }


        public void SetOutput(string outputName)
        {
            if (string.IsNullOrEmpty(outputName))
            {
                outputASelector.name = "[none]";
            }
            else
            {
                outputASelector.name = outputName;
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2
{
    public partial class PowertrainComponentDrawer : NVP_NUIPropertyDrawer
    {
        public void DrawCommonProperties()
        {
            if (Application.isPlaying)
            {
                PowertrainComponent pc = SerializedPropertyHelper.GetTargetObjectOfProperty(drawer.serializedProperty) as PowertrainComponent;

                if (pc != null)
                {
                    drawer.Label($"{pc.RPM:0.0} RPM | {pc.Torque:0.0} Nm " +
                        $"| {((pc.Torque * pc.RPM) / 9550f):0.0} kW");
                }
            }

            drawer.BeginSubsection("Common Properties");
            drawer.Field("name");

            if (drawer.serializedProperty.type != "WheelComponent") // WheelComponent does not support output.
            {
                drawer.Field("inertia");
                drawer.IncreaseIndent();

                drawer.Label("Output To:");
                drawer.IncreaseIndent();
                DrawPowertrainOutputSection(ref drawer.positionRect, drawer.serializedProperty);
                drawer.DecreaseIndent();
            }

            drawer.DecreaseIndent();
            drawer.EndSubsection();
        }


        public virtual void DrawPowertrainOutputSection(ref Rect rect, SerializedProperty property)
        {
            drawer.Field("outputASelector");
        }


        public virtual int GetOutputCount()
        {
            return 1;
        }
    }
}

#endif
