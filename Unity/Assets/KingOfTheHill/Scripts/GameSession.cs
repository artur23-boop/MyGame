using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WinterYard
{
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance {get;private set;}
        public static readonly List<Vector3> Snowbanks=new List<Vector3>();
        public readonly List<Fighter> Fighters=new List<Fighter>();
        public Camera View {get;private set;}
        public bool Running => phase==Phase.Playing;
        public bool InMainMenu => phase==Phase.Welcome||phase==Phase.Settings;
        public int Target {get;private set;}=30;
        public int Difficulty {get;private set;}=1;
        public float Sensitivity {get;private set;}=2;
        enum Phase {Welcome,Settings,Countdown,Playing,Paused,Result}
        Phase phase;
        float countdown,volume=.4f;
        Fighter winner;
        AudioSource speaker;
        GUIStyle title,label,small,button;
        Texture2D pixel;
        readonly Dictionary<int,AudioClip> tones=new Dictionary<int,AudioClip>();
        bool initialized;
        readonly MenuArtwork menuArtwork=new MenuArtwork();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            // Scene generation in the editor is convenient, but the runtime is also self-contained.
            SceneManager.sceneLoaded-=EnsureSession;
            SceneManager.sceneLoaded+=EnsureSession;
        }
        static void EnsureSession(Scene scene,LoadSceneMode mode)
        {
            if(!FindFirstObjectByType<GameSession>())new GameObject("Winter Yard").AddComponent<GameSession>();
        }
        void Awake()
        {
            if(Instance&&Instance!=this){Destroy(gameObject);return;}
            Instance=this;Time.timeScale=1;Snowbanks.Clear();
            var world=new GameObject("Procedural winter yard");world.transform.SetParent(transform);
            Arena.Build(world.transform);
            // Avoid duplicate cameras/audio listeners if starting from a default Unity scene.
            foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))camera.gameObject.SetActive(false);
            var viewObj=new GameObject("Third person camera");viewObj.transform.SetParent(transform);
            View=viewObj.AddComponent<Camera>();View.tag="MainCamera";View.fieldOfView=65;View.farClipPlane=130;
            View.clearFlags=CameraClearFlags.SolidColor;View.backgroundColor=new Color(.24f,.34f,.46f);
            viewObj.AddComponent<AudioListener>();viewObj.AddComponent<YardCamera>();
            speaker=gameObject.AddComponent<AudioSource>();speaker.spatialBlend=0;
            MakeFighters();phase=Phase.Welcome;CursorState(false);initialized=true;
        }
        void MakeFighters()
        {
            foreach(var actor in Fighters)if(actor)Destroy(actor.gameObject);
            Fighters.Clear();
            SnowSurface.ResetPacking();
            if(SnowFootprints.Instance)SnowFootprints.Instance.Clear();
            foreach(var shot in FindObjectsByType<Snowball>(FindObjectsSortMode.None))Destroy(shot.gameObject);
            string[] names={"Ты","Мишка","Санёк","Димон"};
            Color[] colors={new Color(.9f,.64f,.25f),new Color(.72f,.25f,.28f),new Color(.27f,.6f,.5f),new Color(.46f,.38f,.69f)};
            for(int i=0;i<4;i++)
            {
                float a=Mathf.PI+i*Mathf.PI/2;
                var obj=new GameObject(names[i]);obj.transform.SetParent(transform);
                obj.transform.position=new Vector3(Mathf.Sin(a)*11,1.1f,Mathf.Cos(a)*11);
                var fighter=obj.AddComponent<Fighter>();fighter.Initialize(names[i],colors[i],i==0);Fighters.Add(fighter);
            }
            var cam=View.GetComponent<YardCamera>();cam.Target=Fighters[0];
            View.transform.position=Fighters[0].transform.position+new Vector3(0,4,-7);
        }
        void StartMatch()
        {
            Time.timeScale=1;MakeFighters();winner=null;countdown=3;phase=Phase.Countdown;CursorState(true);
        }
        void Update()
        {
            if(phase==Phase.Countdown){countdown-=Time.deltaTime;if(countdown<=0){phase=Phase.Playing;Beep(650,.2f);}}
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(phase==Phase.Playing)Pause();
                else if(phase==Phase.Paused)Resume();
                else if(phase==Phase.Settings||phase==Phase.Countdown)ReturnToMenu();
            }
        }
        void OnApplicationFocus(bool focused){if(!focused&&Running)Pause();}
        void Pause(){foreach(var fighter in Fighters)fighter.CancelThrowCharge();phase=Phase.Paused;Time.timeScale=0;CursorState(false);}
        void Resume(){phase=Phase.Playing;Time.timeScale=1;CursorState(true);}
        public void ReturnToMenu()
        {
            // Keep menu navigation separate from closing the application.
            phase=Phase.Welcome;Time.timeScale=1;countdown=0;winner=null;
            CursorState(false);MakeFighters();
        }
        static void CloseGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        static void CursorState(bool locked){Cursor.lockState=locked?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!locked;}
        public void Finish(Fighter actor)
        {
            if(!Running)return;
            winner=actor;phase=Phase.Result;Time.timeScale=0;CursorState(false);
            if(actor.IsPlayer){PlayerPrefs.SetInt("WinterYardWins",PlayerPrefs.GetInt("WinterYardWins",0)+1);PlayerPrefs.Save();}
            Beep(actor.IsPlayer?740:180,.3f);
        }
        public static bool NearSnowbank(Vector3 point)
        {
            foreach(var p in Snowbanks)if(Vector3.ProjectOnPlane(point-p,Vector3.up).sqrMagnitude<16)return true;
            return false;
        }
        public static Vector3 ClosestSnowbank(Vector3 point)
        {
            var best=Vector3.zero;float distance=float.MaxValue;
            foreach(var p in Snowbanks){float d=(p-point).sqrMagnitude;if(d<distance){distance=d;best=p;}}
            return best;
        }
        public void Beep(int frequency,float duration,float gain=.1f)
        {
            if(volume<=0||!speaker)return;
            int key=frequency*1000+Mathf.RoundToInt(duration*1000);
            if(!tones.TryGetValue(key,out var clip))
            {
                const int rate=22050;int count=Mathf.CeilToInt(rate*duration);var samples=new float[count];
                for(int i=0;i<count;i++){float t=(float)i/rate;samples[i]=Mathf.Sin(t*frequency*Mathf.PI*2)*Mathf.Pow(1-(float)i/count,2);}
                clip=AudioClip.Create("Winter yard tone",count,1,rate,false);clip.SetData(samples,0);tones[key]=clip;
            }
            speaker.PlayOneShot(clip,volume*gain);
        }
        void Styles()
        {
            if(title!=null)return;
            pixel=new Texture2D(1,1);pixel.SetPixel(0,0,Color.white);pixel.Apply();
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label=new GUIStyle(GUI.skin.label){font=font,fontSize=19,wordWrap=true};label.normal.textColor=new Color(.9f,.94f,1);
            small=new GUIStyle(label){fontSize=14};
            title=new GUIStyle(label){fontSize=43,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=19,fixedHeight=45};
        }
        void Box(Rect rect,Color color){var before=GUI.color;GUI.color=color;GUI.DrawTexture(rect,pixel);GUI.color=before;}
        void Text(string text,GUIStyle style=null){GUILayout.Label(text,style??label);}
        void OnGUI()
        {
            if(!initialized||Fighters.Count==0)return;Styles();
            var old=GUI.matrix;
            if(InMainMenu)
            {
                GUI.matrix=Matrix4x4.identity;
                menuArtwork.Draw(new Rect(0,0,Screen.width,Screen.height));
            }
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-800*scale)/2),Quaternion.identity,Vector3.one*scale);
            if(phase==Phase.Playing||phase==Phase.Countdown)
            {
                var player=Fighters[0];Box(new Rect(25,25,290,155),new Color(.04f,.09f,.15f,.87f));
                GUILayout.BeginArea(new Rect(43,38,255,135));Text("ЦАРЬ ГОРЫ • ЗИМНИЙ ДВОР",small);
                Text(player.Score.ToString("0.0")+" / "+Target+" сек",title);
                Text(player.OnSummit?"★ Ты на вершине!":"Заберись в ледяной круг",small);GUILayout.EndArea();
                Box(new Rect(38,165,260,4),new Color(.3f,.4f,.5f));Box(new Rect(38,165,260*Mathf.Clamp01(player.Score/Target),4),new Color(.95f,.7f,.3f));
                GUILayout.BeginArea(new Rect(925,30,330,150));for(int i=1;i<Fighters.Count;i++)Text(Fighters[i].DisplayName+"  "+Fighters[i].Score.ToString("0.0")+" сек"+(Fighters[i].Combat.KnockdownRemaining>0?" • лежит "+Fighters[i].Combat.KnockdownRemaining.ToString("0.0")+" с":""));GUILayout.EndArea();
                GUILayout.BeginArea(new Rect(25,670,1220,120));
                Text("ЛКМ: толчок "+(player.PushCooldown>0?player.PushCooldown.ToString("0.0"):"готов")+"    SHIFT: рывок "+(player.DashCooldown>0?player.DashCooldown.ToString("0.0"):"готов")+"    Снежок: "+(player.HasSnow?"удерживай ПКМ → отпусти":"перезарядка "+player.Combat.ReloadRemaining.ToString("0.0")+" с"));
                Text("Под ногами: "+(player.Surface.IsIce?"ЛЁД — скользко":player.Surface.Depth>.2f?"РЫХЛЫЙ СНЕГ — вязнет":"УТОПТАННЫЙ СНЕГ")+" • глубина "+Mathf.RoundToInt(player.Surface.Depth*100)+" см",small);
                Text(player.Combat.SuperReady?"★ СУПЕРСНЕЖОК — удерживай Q и отпусти":"Комбо: "+player.Combat.ComboHits+" / "+CombatState.ComboTarget+(player.Combat.ComboHits>0?" • продолжи за "+player.Combat.ComboRemaining.ToString("0.0")+" с":" • попади 3 раза"),small);
                if(player.Combat.KnockdownRemaining>0)Text("Сбит с ног! Подъём через "+player.Combat.KnockdownRemaining.ToString("0.0")+" с");
                else if(player.IsRagdoll)Text("Поднимаемся…");
                else if(player.Stun>0)Text("Поднимайся, весь двор смотрит!");
                GUILayout.EndArea();
                GUI.Label(new Rect(460,580,500,28),"НАВЕС: "+Mathf.RoundToInt(player.ThrowAngle)+"° • колесо мыши — высота дуги",small);
                if(player.IsCharging)
                {
                    Box(new Rect(480,635,320,7),new Color(.15f,.25f,.35f,.9f));
                    Box(new Rect(480,635,320*player.ThrowPower,7),player.PreviewSuper?new Color(1,.7f,.18f):new Color(.65f,.9f,1));
                    GUI.Label(new Rect(480,608,360,25),"Сила: "+Mathf.RoundToInt(player.ThrowPower*100)+"% • отпусти для броска",small);
                }
                // Screen crosshair complements the actual ballistic preview.
                Box(new Rect(638,397,4,6),new Color(1,.85f,.5f,.9f));Box(new Rect(635,400,10,2),new Color(1,.85f,.5f,.9f));
                if(phase==Phase.Countdown){GUI.Label(new Rect(550,290,200,90),Mathf.CeilToInt(countdown).ToString(),new GUIStyle(title){fontSize=80,alignment=TextAnchor.MiddleCenter});}
            }
            else
            {
                if(!InMainMenu)Box(new Rect(0,0,1280,800),new Color(.03f,.07f,.12f,.82f));
                GUILayout.BeginArea(InMainMenu?new Rect(65,65,470,685):new Rect(365,65,550,685));
                Text("ЗИМНИЙ ДВОР • ДЕКАБРЬ 1998",small);GUILayout.Space(12);
                Text(phase==Phase.Result?(winner.IsPlayer?"ТЫ — ЦАРЬ ГОРЫ!":winner.DisplayName+" — царь горы"):phase==Phase.Paused?"ГРЕЕМ ВАРЕЖКИ":phase==Phase.Settings?"НАСТРОЙКИ":"ЦАРЬ ГОРЫ",title);
                GUILayout.Space(15);
                if(phase==Phase.Welcome)
                {
                    Text("Добро пожаловать во двор!");GUILayout.Space(8);
                    Text("Свежий снег. Три соперника. Одна вершина.\nУдержи гору и стань царём этого двора.");
                    GUILayout.Space(30);
                    if(GUILayout.Button("ИГРАТЬ — ВЫЙТИ ВО ДВОР",button))StartMatch();
                    GUILayout.Space(8);
                    if(GUILayout.Button("НАСТРОЙКИ",button))phase=Phase.Settings;
                    GUILayout.Space(8);
                    if(GUILayout.Button("ЗАКРЫТЬ ИГРУ",button))CloseGame();
                    GUILayout.Space(25);
                    Text("Побед во дворе: "+PlayerPrefs.GetInt("WinterYardWins",0),small);
                    Text("WASD — движение • мышь — обзор\nЛКМ — толчок • удерживай/отпусти ПКМ — снежок\nУдерживай/отпусти Q — супер • колесо — высота дуги\nSpace — прыжок • Shift — рывок\nEsc в матче — пауза и выход в главное меню",small);
                }
                else if(phase==Phase.Settings)
                {
                    Text("Сложность",small);Difficulty=GUILayout.SelectionGrid(Difficulty,new[]{"Легко","Средне","Сложно"},3,button);
                    Text("Секунд до победы",small);int index=Target==20?0:Target==30?1:2;index=GUILayout.SelectionGrid(index,new[]{"20","30","60"},3,button);Target=index==0?20:index==1?30:60;
                    Text("Громкость",small);volume=GUILayout.HorizontalSlider(volume,0,1);
                    Text("Чувствительность мыши",small);Sensitivity=GUILayout.HorizontalSlider(Sensitivity,.5f,5);
                    GUILayout.Space(25);
                    if(GUILayout.Button("НАЗАД В ГЛАВНОЕ МЕНЮ",button))ReturnToMenu();
                }
                else
                {
                    if(phase==Phase.Result){foreach(var actor in Fighters)Text(actor.DisplayName+" — "+actor.Score.ToString("0.0")+" сек");GUILayout.Space(20);if(GUILayout.Button("ЕЩЁ ОДИН РАУНД",button))StartMatch();}
                    else if(GUILayout.Button("ПРОДОЛЖИТЬ",button))Resume();
                    if(GUILayout.Button("ВЫХОД В ГЛАВНОЕ МЕНЮ",button))ReturnToMenu();
                }
                GUILayout.EndArea();
            }
            GUI.matrix=old;
        }
        void OnDestroy()
        {
            if(Instance!=this)return;Time.timeScale=1;CursorState(false);Instance=null;
            foreach(var tone in tones.Values)if(tone)Destroy(tone);
            if(pixel)Destroy(pixel);
            menuArtwork.Dispose();
        }
    }
}
