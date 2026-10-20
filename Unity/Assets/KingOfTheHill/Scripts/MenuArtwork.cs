using UnityEngine;

namespace WinterYard
{
    // Animated presentation of the generated artwork, independent of game time/physics.
    public sealed class MenuArtwork
    {
        Texture2D background, shade, flake;
        bool loaded;
        void Load()
        {
            if(loaded)return;loaded=true;
            background=Resources.Load<Texture2D>("Menu/WinterCourtyard");
            shade=new Texture2D(128,1,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            for(int x=0;x<128;x++)shade.SetPixel(x,0,new Color(.025f,.055f,.1f,Mathf.Lerp(.94f,0,Mathf.SmoothStep(0,1,x/127f))));
            shade.Apply();
            flake=new Texture2D(16,16,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<16;y++)for(int x=0;x<16;x++)
            {
                float radius=Vector2.Distance(new Vector2(x,y),new Vector2(7.5f,7.5f))/7.5f;
                flake.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(1-radius)*Mathf.Clamp01(1-radius)));
            }
            flake.Apply();
        }
        public void Draw(Rect viewport)
        {
            Load();var saved=GUI.color;GUI.color=Color.white;
            GUI.BeginGroup(viewport);
            float w=viewport.width,h=viewport.height,t=Time.unscaledTime;
            if(background)
            {
                float cover=Mathf.Max(w/background.width,h/background.height);
                float zoom=1.035f+Mathf.Sin(t*.09f)*.006f;
                float width=background.width*cover*zoom,height=background.height*cover*zoom;
                GUI.DrawTexture(new Rect((w-width)*.5f+Mathf.Sin(t*.07f)*w*.004f,(h-height)*.5f,width,height),background,ScaleMode.StretchToFill);
            }
            GUI.DrawTexture(new Rect(0,0,w*.75f,h),shade,ScaleMode.StretchToFill);
            for(int i=0;i<65;i++)
            {
                float seed=Mathf.Repeat(i*.618034f,1),layer=.35f+Mathf.Repeat(i*.381966f,1);
                float size=(3+layer*5)*h/800;
                float x=Mathf.Repeat(seed*w+Mathf.Sin(t*.3f+i)*14+t*layer*7,w+20)-10;
                float y=Mathf.Repeat(Mathf.Repeat(i*.754877f,1)*h+t*(14+layer*23),h+20)-10;
                GUI.color=new Color(.91f,.96f,1,.3f+layer*.2f);
                GUI.DrawTexture(new Rect(x,y,size,size),flake);
            }
            GUI.EndGroup();GUI.color=saved;
        }
        public void Dispose()
        {
            if(shade)Object.Destroy(shade);if(flake)Object.Destroy(flake);
            // background is a shared imported Resources asset, not an owned runtime texture.
        }
    }
}
