using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lexi
{
    [CreateAssetMenu(fileName = "AnimatableProperty_0", menuName = "Lexi/Animatable Property Preset")]
    public class AnimatableProperty : ScriptableObject
    {
        [Header("Settings")]
        [SerializeField] private string m_animationName;
        [SerializeField] private string[] m_propertyNames;
        [SerializeField] private string m_animatableSuffix;
        [SerializeField] private string m_shaderTag;
        [SerializeField] private float m_minValue;
        [SerializeField] private float m_midValue;
        [SerializeField] private float m_maxValue;
        
        public string AnimationName => m_animationName;
        public string[] PropertyNames => m_propertyNames;
        public string AnimatableSuffix => m_animatableSuffix;
        public string ShaderTag => m_shaderTag;
        public float MinValue => m_minValue;
        public float MidValue => m_midValue;
        public float MaxValue => m_maxValue;

        public void Initialise(string animationName, string[] propertyNames, string animatableSuffix, string shaderTag, float minValue, float midValue, float maxValue)
        {
            m_animationName = animationName;
            m_propertyNames = propertyNames;
            m_animatableSuffix = animatableSuffix;
            m_shaderTag = shaderTag;
            m_minValue = minValue;
            m_midValue = midValue;
            m_maxValue = maxValue;
        }
    }
}
