using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;


namespace TMPAnimatedEffects
{
    [RequireComponent(typeof(TMP_Text))]
    public class TMPAnimatedEffects : MonoBehaviour
    {
        [SerializeField] [TextArea(5, 10)] private string _textInput;

        private TMP_Text _textComponent;
        private TMP_TextInfo _textInfo;
        private string _rawText;

        private float _startTime;
        private float _typewriterStartTime;
        private bool _useTypewriter;
        private float _typewriterSpeed = 50f;

        private readonly List<EffectRegion> _effectRegions = new();

        private struct EffectRegion
        {
            public int StartIndex;
            public int Length;
            public string Type;
            public Dictionary<string, float> Parameters;
        }

        private void Awake()
        {
            Initialize();
        }
        
        public void Reboot()
        {
            Initialize();
        }

        private void Initialize()
        {
            _textComponent = GetComponent<TMP_Text>();

            _rawText = !string.IsNullOrEmpty(_textInput) ? _textInput : _textComponent.text;

            ParseTags();
            _textComponent.ForceMeshUpdate();
            _startTime = Time.time;
            _typewriterStartTime = Time.time;
        }

        private void Update()
        {
            AnimateEffects();
        }

        private void ParseTags()
        {
            _effectRegions.Clear();
            var working = _rawText;

            var typewriterFullMatch = Regex.Match(working,
                @"\[typewriter(.*?)\](.*?)\[/typewriter\]", RegexOptions.Singleline);

            if (typewriterFullMatch.Success)
            {
                _useTypewriter = true;

                var paramMatch = Regex.Match(typewriterFullMatch.Groups[1].Value, @"speed=([\d.]+)");
                if (paramMatch.Success)
                    _typewriterSpeed = float.Parse(paramMatch.Groups[1].Value);

                working = typewriterFullMatch.Groups[2].Value;
            }

            var pattern = @"\[(wave|shake|glitch|fade|rainbow)(.*?)\](.*?)\[/\1\]";
            var matches = Regex.Matches(working, pattern, RegexOptions.Singleline);

            var offset = 0;

            foreach (Match match in matches)
            {
                var tagType = match.Groups[1].Value;
                var paramStr = match.Groups[2].Value;
                var innerText = match.Groups[3].Value;

                Dictionary<string, float> parameters = new();

                var paramMatches = Regex.Matches(paramStr, @"(\w+)=([\d.]+)");
                foreach (Match p in paramMatches)
                    parameters[p.Groups[1].Value] = float.Parse(p.Groups[2].Value);

                var startIndex = match.Index - offset;

                _effectRegions.Add(new EffectRegion
                {
                    StartIndex = startIndex,
                    Length = innerText.Length,
                    Type = tagType,
                    Parameters = parameters
                });

                working = working.Remove(match.Index - offset, match.Length)
                    .Insert(match.Index - offset, innerText);

                offset += match.Length - innerText.Length;
            }

            _textComponent.text = working;
        }

        private void AnimateEffects()
        {
            _textComponent.ForceMeshUpdate();
            _textInfo = _textComponent.textInfo;

            for (var i = 0; i < _textInfo.characterCount; i++)
            {
                if (!_textInfo.characterInfo[i].isVisible)
                {
                    continue;
                }

                if (_useTypewriter)
                {
                    var elapsed = Time.time - _typewriterStartTime;
                    var visibleCount = Mathf.FloorToInt(elapsed * _typewriterSpeed);
                    if (i >= visibleCount)
                    {
                        var vector3S = _textInfo.meshInfo[_textInfo.characterInfo[i].materialReferenceIndex].vertices;
                        var vIndex = _textInfo.characterInfo[i].vertexIndex;
                        for (var j = 0; j < 4; j++)
                            vector3S[vIndex + j] = Vector3.zero;

                        continue;
                    }
                }

                var charInfo = _textInfo.characterInfo[i];
                var vertices = _textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
                var colors = _textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;
                var vertexIndex = charInfo.vertexIndex;

                var offset = Vector3.zero;
                Color32? overrideColor = null;
                byte? overrideAlpha = null;

                foreach (var region in _effectRegions)
                {
                    if (i < region.StartIndex || i >= region.StartIndex + region.Length)
                    {
                        continue;
                    }

                    switch (region.Type)
                    {
                        case "wave":
                        {
                            var amp = region.Parameters.GetValueOrDefault("amp", 5f);
                            var freq = region.Parameters.GetValueOrDefault("freq", 5f);
                            var wave = Mathf.Sin(Time.time * freq + i * 0.2f) * amp;
                            offset = new Vector3(0, wave, 0);
                            break;
                        }
                        case "shake":
                        {
                            var amt = region.Parameters.GetValueOrDefault("amt", 2f);
                            var x = Random.Range(-amt, amt);
                            var y = Random.Range(-amt, amt);
                            offset = new Vector3(x, y, 0);
                            break;
                        }
                        case "glitch":
                        {
                            var intensity = region.Parameters.GetValueOrDefault("intensity", 2f);
                            var x = Mathf.PerlinNoise(i, Time.time * 10f) * intensity - intensity / 2f;
                            var y = Mathf.PerlinNoise(i + 999, Time.time * 10f) * intensity - intensity / 2f;
                            offset = new Vector3(x, y, 0);
                            break;
                        }
                        case "fade":
                        {
                            var duration = region.Parameters.GetValueOrDefault("duration", 2f);
                            var endAlpha = region.Parameters.GetValueOrDefault("endAlpha", 0f);
                            var startAlpha = endAlpha == 0f ? 1f : 0f;
                            var progress = Mathf.Clamp01((Time.time - _startTime) / duration);
                            var alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                            overrideAlpha = (byte)(alpha * 255);
                            break;
                        }
                        case "rainbow":
                        {
                            var speed = region.Parameters.GetValueOrDefault("speed", 2f);
                            var hue = Mathf.Repeat(Time.time * speed + i * 0.1f, 1f);
                            overrideColor = Color.HSVToRGB(hue, 1, 1);
                            break;
                        }
                    }

                    break;
                }

                for (var j = 0; j < 4; j++)
                {
                    vertices[vertexIndex + j] += offset;

                    if (overrideColor.HasValue)
                    {
                        colors[vertexIndex + j] = overrideColor.Value;
                    }

                    if (overrideAlpha.HasValue)
                    {
                        var col = colors[vertexIndex + j];
                        col.a = overrideAlpha.Value;
                        colors[vertexIndex + j] = col;
                    }
                }
            }

            for (var m = 0; m < _textInfo.meshInfo.Length; m++)
            {
                var meshInfo = _textInfo.meshInfo[m];
                meshInfo.mesh.vertices = meshInfo.vertices;
                meshInfo.mesh.colors32 = meshInfo.colors32;
                _textComponent.UpdateGeometry(meshInfo.mesh, m);
            }
        }
    }
}