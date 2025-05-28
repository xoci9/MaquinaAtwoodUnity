using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VerletSimulation;

namespace Demo
{
    // Controlador Unity para montar e simular o efeito de uma corda num sistema Atwood
    public class SimuladorDeCorda : MonoBehaviour
    {
        [Header("Configuração Física")]
        [SerializeField] private float gravidade = 9.8f;
        [SerializeField] private int segmentos = 50;
        [SerializeField] private int iteracoes = 3;
        [SerializeField] private float comprimento = 0.5f;

        [Header("Massa nas Extremidades")]
        [SerializeField] private float massaEsquerda = 3f;
        [SerializeField] private float massaDireita = 3f;

        [Header("Componentes de Simulação")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform startTransform;
        [SerializeField] private GameObject dotPrefab;

        [Header("Visuais das Extremidades")]
        [SerializeField] private GameObject esquerdaVisual;
        [SerializeField] private GameObject direitaVisual;

        [Header("UI para Massa em Tempo Real")]
        [SerializeField] private TMP_InputField leftMassInput;
        [SerializeField] private TMP_InputField rightMassInput;

        [Header("World Selector UI")]
        [SerializeField] private TMP_Dropdown worldDropdown;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite[] worldBackgrounds;
        [SerializeField] private float[] worldGravities;

        [Header("Botão de Reset")]
        [SerializeField] private Button resetButton;

        private Verlet simulador;
        private Dot pontoEsquerda;
        private Dot pontoDireita;
        private float raioPolia;

        private void Awake()
        {
            // Setup LineRenderer
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.green;
            lineRenderer.endColor = Color.red;
            lineRenderer.positionCount = 0;

            // Ensure the dit visuals render on top
            esquerdaVisual.GetComponent<SpriteRenderer>().sortingOrder = 10;
            direitaVisual.GetComponent<SpriteRenderer>().sortingOrder = 10;

            // Hook up Reset button
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetSimulation);

            // Build the rope/simulation
            InitSimulation();
        }

        private void Start()
        {
            // Mass inputs
            if (leftMassInput != null)
            {
                leftMassInput.text = massaEsquerda.ToString();
                leftMassInput.onEndEdit.AddListener(OnLeftMassChanged);
            }
            if (rightMassInput != null)
            {
                rightMassInput.text = massaDireita.ToString();
                rightMassInput.onEndEdit.AddListener(OnRightMassChanged);
            }

            // World dropdown
            if (worldDropdown != null && worldBackgrounds.Length == worldGravities.Length)
            {
                worldDropdown.ClearOptions();
                var names = new System.Collections.Generic.List<string>();
                foreach (var sp in worldBackgrounds) names.Add(sp.name);
                worldDropdown.AddOptions(names);

                worldDropdown.onValueChanged.AddListener(OnWorldChanged);
                OnWorldChanged(worldDropdown.value);
            }
        }

        private void InitSimulation()
        {
            // Clean up any existing visuals (scene instances, not assets)
            if (esquerdaVisual != null) Destroy(esquerdaVisual);
            if (direitaVisual != null) Destroy(direitaVisual);

            // Reinitialize Verlet simulator
            simulador = new Verlet(1f, iteracoes);
            simulador.collisionLayerMask = LayerMask.GetMask("Default", "Environment");
            simulador.dotRadius = 0.2f;

            // Clear the line
            lineRenderer.positionCount = 0;

            // Build rope around pulley
            raioPolia = 0.7f;
            int segmentosArco = segmentos / 3;
            int segmentosCauda = (segmentos - segmentosArco) / 2;
            Vector3 centroPolia = startTransform.position;
            Dot anterior;

            // --- LEFT END (massaEsquerda) ---
            Vector3 posInicio = centroPolia
                                + Vector3.left * raioPolia
                                + Vector3.down * comprimento * segmentosCauda;
            pontoEsquerda = new Dot(posInicio, false, massaEsquerda);
            simulador.Dots.Add(pontoEsquerda);
            AddPositionToRenderer(pontoEsquerda.Currentposition);
            anterior = pontoEsquerda;

            // left tail segments
            for (int i = 0; i < segmentosCauda - 1; i++)
            {
                Vector3 p = anterior.Currentposition + Vector3.up * comprimento;
                Dot d = new Dot(p, false, 1f);
                Dot.Connect(anterior, d);
                simulador.Dots.Add(d);
                AddPositionToRenderer(d.Currentposition);
                anterior = d;
            }

            // pulley arc
            for (int i = 0; i <= segmentosArco; i++)
            {
                float t = (float)i / segmentosArco;
                float ang = Mathf.PI * (1 - t);
                Vector3 off = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * raioPolia;
                Dot d = new Dot(centroPolia + off, false, 1f);
                Dot.Connect(anterior, d);
                simulador.Dots.Add(d);
                AddPositionToRenderer(d.Currentposition);
                anterior = d;
            }

            // right tail segments
            for (int i = 0; i < segmentosCauda - 1; i++)
            {
                Vector3 p = anterior.Currentposition + Vector3.down * comprimento;
                Dot d = new Dot(p, false, 1f);
                Dot.Connect(anterior, d);
                simulador.Dots.Add(d);
                AddPositionToRenderer(d.Currentposition);
                anterior = d;
            }

            // --- RIGHT END (massaDireita) ---
            Vector3 posFinal = anterior.Currentposition + Vector3.down * comprimento;
            pontoDireita = new Dot(posFinal, false, massaDireita);
            Dot.Connect(anterior, pontoDireita);
            simulador.Dots.Add(pontoDireita);
            AddPositionToRenderer(pontoDireita.Currentposition);

            // Instantiate visuals from prefab
            if (dotPrefab != null)
            {
                esquerdaVisual = Instantiate(esquerdaVisual, pontoEsquerda.Currentposition, Quaternion.identity);
                direitaVisual = Instantiate(direitaVisual, pontoDireita.Currentposition, Quaternion.identity);
            }
        }

        public void ResetSimulation()
        {
            // only rebuild rope & visuals, leave UI entries alone
            InitSimulation();
        }

        private void OnLeftMassChanged(string txt)
        {
            if (float.TryParse(txt, out float m))
            {
                massaEsquerda = Mathf.Max(0.01f, m);
                if (pontoEsquerda != null) pontoEsquerda.mass = massaEsquerda;
            }
        }

        private void OnRightMassChanged(string txt)
        {
            if (float.TryParse(txt, out float m))
            {
                massaDireita = Mathf.Max(0.01f, m);
                if (pontoDireita != null) pontoDireita.mass = massaDireita;
            }
        }

        private void OnWorldChanged(int idx)
        {
            if (idx < 0 || idx >= worldBackgrounds.Length) return;
            if (backgroundImage != null) backgroundImage.sprite = worldBackgrounds[idx];
            gravidade = worldGravities[idx];
        }

        private void FixedUpdate()
        {
            int steps = 4;
            float dt = Time.fixedDeltaTime / steps;

            for (int i = 0; i < steps; i++)
            {
                simulador.AddForce((gravidade / steps) * Vector3.down);
                simulador.simular(dt);
            }

            // clamp both ends at top of pulley
            float maxY = startTransform.position.y + raioPolia;

            Vector3 pL = pontoEsquerda.Currentposition;
            if (pL.y > maxY)
            {
                pL.y = maxY;
                pontoEsquerda.Currentposition = pL;
                pontoEsquerda.LastPosition = pL;
                pontoEsquerda.isLocked = true;
            }
            else pontoEsquerda.isLocked = false;

            Vector3 pR = pontoDireita.Currentposition;
            if (pR.y > maxY)
            {
                pR.y = maxY;
                pontoDireita.Currentposition = pR;
                pontoDireita.LastPosition = pR;
                pontoDireita.isLocked = true;
            }
            else pontoDireita.isLocked = false;

            // redraw line
            for (int i = 0; i < simulador.Dots.Count; i++)
                lineRenderer.SetPosition(i, simulador.Dots[i].Currentposition);

            // update visual GameObjects
            if (esquerdaVisual != null)
                esquerdaVisual.transform.position = pontoEsquerda.Currentposition;
            if (direitaVisual != null)
                direitaVisual.transform.position = pontoDireita.Currentposition;
        }

        private void AddPositionToRenderer(Vector3 pos)
        {
            int c = lineRenderer.positionCount;
            lineRenderer.positionCount = c + 1;
            lineRenderer.SetPosition(c, pos);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || simulador == null) return;

            Gizmos.color = Color.magenta;
            foreach (var d in simulador.Dots)
                Gizmos.DrawWireSphere(d.Currentposition, simulador.dotRadius);

            Gizmos.color = Color.yellow;
            foreach (var d in simulador.Dots)
                foreach (var con in d.connections)
                    Gizmos.DrawLine(con.dotA.Currentposition, con.dotB.Currentposition);
        }
    }
}
