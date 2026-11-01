"""Parent voice recipe ("F_warm_body_strong").
A voice guide (spoken syllables) steers a blend of animal/noise carriers:
the guide gives loudness and melody only; a low-passed copy of the guide
adds letterless warmth. Then glue chain, warmth, body, depth."""
import numpy as np, soundfile as sf, glob, random
SR=48000; N=2048; H=256; WIN=np.hanning(N)
VOICE="D:/Projects/O-YUNG-AudioWork/Voice"
CAT=VOICE+"/Animals/purr_cat_badideaproductions_856508.wav"

def load(p):
    x,sr=sf.read(p,always_2d=True); x=x.mean(1)
    if sr!=SR:
        t=np.arange(0,len(x)/sr,1/SR); x=np.interp(t,np.arange(len(x))/sr,x)
    return x
def stft(x):
    x=np.pad(x,(N,N)); return np.array([np.fft.rfft(WIN*x[i:i+N]) for i in range(0,len(x)-N,H)])
def istft(S,n):
    y=np.zeros(n+3*N); ws=np.zeros_like(y)
    for k,s in enumerate(S):
        i=k*H; y[i:i+N]+=WIN*np.fft.irfft(s); ws[i:i+N]+=WIN**2
    return (y/np.maximum(ws,1e-6))[N:N+n]
def follow(x,at,rl):
    a,r=np.exp(-1/(at*SR)),np.exp(-1/(rl*SR)); o=np.zeros_like(x); s=0
    for i,v in enumerate(np.abs(x)):
        s=a*s+(1-a)*v if v>s else r*s+(1-r)*v; o[i]=s
    return o
def compress(x,thr_db,ratio,at,rl):
    env=follow(x,at,rl); thr=10**(thr_db/20)*np.abs(x).max()
    with np.errstate(divide='ignore'):
        g=np.where(env>thr,(env/thr)**(1/ratio-1),1.0)
    return x*g
def eq(x,curve):
    S=np.fft.rfft(x); f=np.fft.rfftfreq(len(x),1/SR); return np.fft.irfft(S*curve(f),len(x))
def db(g): return 10**(g/20)
def bell(f,fc,g,q): return g*np.exp(-0.5*(np.log2(np.maximum(f,1)/fc)*q)**2)
def noise_ir(sec,decay,seed,lp):
    t=np.arange(int(sec*SR)); r=np.random.default_rng(seed).standard_normal(len(t))*np.exp(-t/(decay*SR))
    return eq(r,lambda f: 1/np.sqrt(1+(f/lp)**4))

def f0track(v):
    hop=int(0.01*SR); w=int(0.04*SR); out=[]
    for i in range(0,max(1,len(v)-w),hop):
        f=v[i:i+w]-v[i:i+w].mean()
        if len(f)<w or np.sqrt((f**2).mean())<3e-3: out.append(0); continue
        c=np.correlate(f,f,'full')[w-1:]; lo,hi=int(SR/500),int(SR/70)
        k=lo+np.argmax(c[lo:hi]); out.append(SR/k if c[k]>0.3*c[0] else 0)
    return np.array(out,float)
def fill(f,ref):
    last=ref
    for i in range(len(f)):
        if f[i]>0: last=f[i]
        else: f[i]=last
    return np.convolve(f,np.ones(7)/7,'same')
def loudest(c,dur):
    h=int(0.01*SR); r=np.array([np.sqrt((c[i:i+h]**2).mean()) for i in range(0,len(c)-h,h)])
    n=max(1,int(dur/0.01)); s=np.convolve(r,np.ones(n),'valid')
    i=int(np.argmax(s))*h; seg=c[i:i+int(dur*SR)]
    return seg/(np.sqrt((seg**2).mean())+1e-9)
def rms_loud(x):
    h=int(.02*SR); r=np.array([np.sqrt((x[i:i+h]**2).mean()) for i in range(0,len(x)-h,h)])
    return np.sqrt((r[r>=np.percentile(r,60)]**2).mean())+1e-9
def ltspec(x):
    m=np.log(np.abs(stft(x)).mean(0)+1e-9); c=np.fft.irfft(m); c[20:-20]=0
    return np.fft.rfft(c).real

def carriers():
    """Tone-matched (halfway) then cross-pollinated (35%) carrier library."""
    files=sorted(f.replace("\\","/") for f in glob.glob(VOICE+"/Carriers/*.wav")+glob.glob(VOICE+"/Animals/*.wav"))
    src={f:loudest(load(f),4.0) for f in files}
    specs={f:ltspec(x) for f,x in src.items()}; target=np.mean(list(specs.values()),0)
    lib={}
    for f,x in src.items():
        y=istft(stft(x)*np.exp(0.5*(target-specs[f]))[None,:],len(x)); lib[f]=y/rms_loud(y)
    rng=random.Random(11); out={}
    for k in lib:
        p=rng.choice([q for q in lib if q!=k]); a=lib[k]; b=np.resize(lib[p],len(a))
        A=stft(a); B=stft(b); M=np.abs(A)**0.65*(np.abs(B)+1e-9)**0.35
        y=istft(M*np.exp(1j*np.angle(A)),len(a)); out[k]=y/rms_loud(y)
    return out

def speak(guide,lib,body=1.0,hum_amt=0.15,seed=3,floor="cat",drift_amt=0.75):
    """guide: mono voice at SR. Returns stereo float array."""
    v=guide; n=len(v)
    f=f0track(v); med=np.median(f[f>0]); f=fill(f,med)
    pos=np.cumsum(np.interp(np.arange(n),np.arange(len(f))*0.01*SR,f/med))
    e=follow(v,.005,.05); e=(e/e.max())**0.8
    def stream(x): p=pos%(len(x)-1); i=p.astype(int); r=p-i; return x[i]*(1-r)+x[i+1]*r
    others=[k for k in lib if k!=CAT]  # floor source is never in the drift
    rng=np.random.default_rng(seed); k=int(n/SR*3.2)+4
    M=np.array([np.interp(np.arange(n),np.linspace(0,n,k),rng.standard_normal(k)) for _ in others])
    M=np.exp(3*M); M/=np.sqrt((M**2).sum(0,keepdims=True))
    d=sum(M[i]*stream(lib[o]) for i,o in enumerate(others))
    g=np.clip(follow(d,.03,.08)/(follow(d,.001,.01)+1e-9),0,1)**0.9      # soften blips
    fr=np.fft.rfftfreq(N,1/SR)
    d=istft(stft(d*g)*np.where(fr<4000,1,np.maximum(0.4,1-(fr-4000)/8000))[None,:],n)
    b=drift_amt*d*e
    if isinstance(floor,str): b=b+0.6*stream(lib[CAT])*e
    elif floor is not None:   # already follows the voice; match its level
        b=b+0.6*floor/(rms_loud(floor)+1e-9)*rms_loud(d*e)
    S=stft(v); hv=istft(S*(np.clip(1-(fr-700)/700,0,1)**2)[None,:],n)      # letterless hum
    hv*=np.clip(follow(hv,.04,.1)/(follow(hv,.002,.02)+1e-9),0,1)
    y=b+hv/(np.sqrt((hv**2).mean())+1e-12)*np.sqrt((b**2).mean())*hum_amt
    # glue chain: resonance taming + blur, multiband, saturation, bus comp
    S=stft(y); Mg=np.abs(S); P=np.angle(S); kk=np.hanning(5); kk/=kk.sum()
    Mg=np.apply_along_axis(lambda c: np.convolve(c,kk,'same'),0,Mg)
    L=np.log(Mg+1e-9); c=np.fft.irfft(L,axis=1); c[:,30:-30]=0; sm=np.exp(np.fft.rfft(c,axis=1).real)
    Mg=np.minimum(Mg,sm*1.4+0.3*np.maximum(Mg-sm*1.4,0)); y=istft(Mg*np.exp(1j*P),n)
    S=stft(y); lo=np.clip((450-fr)/300,0,1); hi=np.clip((fr-3000)/1500,0,1); mid=np.clip(1-lo-hi,0,1)
    y=sum(compress(istft(S*m[None,:],n),t,3,.01,.12) for m,t in ((lo,-14),(mid,-12),(hi,-16)))
    y=np.tanh(1.3*y/np.abs(y).max()); y=compress(y,-10,2.5,.02,.2)
    w=np.convolve(y,noise_ir(.5,.1,1,20000))[:n]; w*=np.sqrt((y**2).mean()/((w**2).mean()+1e-12))
    y=y+0.18*w; y/=np.abs(y).max()
    return warm_body(y,body)

def warm_body(y,body=1.0):
    """Flatten, warm EQ, even harmonics, chest cavity, dark stereo depth."""
    n=len(y)
    y=compress(y,-20,3,.03,.3)
    y=eq(y,lambda f: db(bell(f,250,3.5,1.2)+bell(f,120,2,1.5)-np.clip((f-4000)/6000,0,1)*6))
    y=np.tanh(1.4*y/np.abs(y).max()+0.12)-np.tanh(0.12)
    cut=3200-1400*body
    y=eq(y,lambda f: db(bell(f,180,4*body,2)+bell(f,430,3*body,2.5))/np.sqrt(1+(f/cut)**4))
    ch=np.convolve(y,noise_ir(.06,.015,5,1200))[:n]; ch*=np.sqrt((y**2).mean()/((ch**2).mean()+1e-12))
    y=y+0.5*body*ch
    pd=int(.025*SR); Lr=np.convolve(y,noise_ir(1.2,.28,7,2500))[:n]; Rr=np.convolve(y,noise_ir(1.2,.28,8,2500))[:n]
    s=np.sqrt((y**2).mean()/((Lr**2).mean()+1e-12)); Lr=np.roll(Lr*s,pd); Rr=np.roll(Rr*s,pd)
    out=np.stack([y+0.22*Lr,y+0.22*Rr],1); return 0.75*out/np.abs(out).max()
