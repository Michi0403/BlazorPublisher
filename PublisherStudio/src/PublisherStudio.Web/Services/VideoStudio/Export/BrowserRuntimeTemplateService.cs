namespace PublisherStudio.Services.VideoStudio.Export;

/// <summary>
/// Defines the browser runtime template service contract.
/// </summary>
public interface IBrowserRuntimeTemplateService
{
    string CreateBlobRuntime(string payload);
}

/// <summary>
/// Provides browser runtime template service operations.
/// </summary>
public sealed class BrowserRuntimeTemplateService : IBrowserRuntimeTemplateService
{
    /// <summary>
    /// Creates blob runtime.
    /// </summary>
    public string CreateBlobRuntime(string payload) => """
(() => {
  const config = __PUBLISHERSTUDIO_BLOB_RUNTIME_PAYLOAD__;
  const canvas = document.querySelector('.publisher-3d-blob');
  if (!(canvas instanceof HTMLCanvasElement)) return;
  const ctx = canvas.getContext('2d');
  if (!ctx) return;
  const clamp = (v,a,b) => Math.max(a,Math.min(b,Number.isFinite(Number(v))?Number(v):a));
  const resample = (points,count) => {
    if (!Array.isArray(points) || points.length < 2) return [];
    const lengths=[]; let total=0;
    for(let i=0;i<points.length;i++){const a=points[i],b=points[(i+1)%points.length];const len=Math.hypot(b[0]-a[0],b[1]-a[1]);lengths.push(len);total+=len;}
    if(total<=1e-6) return Array.from({length:count},()=>points[0].slice());
    const result=[];
    for(let i=0;i<count;i++){let distance=total*i/count,edge=0;while(edge<lengths.length-1&&distance>lengths[edge]){distance-=lengths[edge];edge++;}const a=points[edge],b=points[(edge+1)%points.length],t=lengths[edge]>0?distance/lengths[edge]:0;result.push([a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t]);}
    return result;
  };
  const count=Math.max(3,Math.min(128,Math.max(config.source.length,config.target.length)));
  const source=resample(config.source,count),target=resample(config.target,count);
  const resize=()=>{const ratio=Math.min(2,Math.max(1,window.devicePixelRatio||1));const w=Math.max(2,Math.round(canvas.clientWidth*ratio)),h=Math.max(2,Math.round(canvas.clientHeight*ratio));if(canvas.width!==w||canvas.height!==h){canvas.width=w;canvas.height=h;}};
  const path=(points,offsetX=0,offsetY=0)=>{ctx.beginPath();points.forEach((point,index)=>{const x=(.08+.84*point[0])*canvas.width+offsetX;const y=(.08+.84*point[1])*canvas.height+offsetY;if(index===0)ctx.moveTo(x,y);else ctx.lineTo(x,y);});ctx.closePath();};
  const draw=time=>{resize();ctx.clearRect(0,0,canvas.width,canvas.height);const phase=config.animate&&config.morphEnabled?(Math.sin(time*.001*config.speed*Math.PI)+1)/2:clamp(config.morphAmount,0,1);const points=source.map((point,index)=>[point[0]+(target[index][0]-point[0])*phase,point[1]+(target[index][1]-point[1])*phase]);const depth=Math.max(2,Math.round(Math.min(canvas.width,canvas.height)*config.depth*.22));for(let step=depth;step>0;step--){path(points,step*.62,step*.78);ctx.fillStyle=`rgba(2,20,42,${.18+.48*(1-step/depth)})`;ctx.fill();}path(points);const gradient=ctx.createLinearGradient(0,0,canvas.width,canvas.height);gradient.addColorStop(0,'rgba(125,211,252,.98)');gradient.addColorStop(.48,'rgba(14,165,233,.92)');gradient.addColorStop(1,'rgba(30,64,175,.96)');ctx.globalAlpha=clamp(config.opacity,0,1);ctx.fillStyle=gradient;ctx.fill();ctx.save();ctx.clip();const shine=ctx.createRadialGradient(canvas.width*.32,canvas.height*.25,0,canvas.width*.32,canvas.height*.25,Math.max(canvas.width,canvas.height)*.7);shine.addColorStop(0,'rgba(255,255,255,.62)');shine.addColorStop(.35,'rgba(255,255,255,.08)');shine.addColorStop(1,'rgba(0,0,0,.34)');ctx.fillStyle=shine;ctx.fillRect(0,0,canvas.width,canvas.height);ctx.restore();ctx.globalAlpha=1;requestAnimationFrame(draw);};
  requestAnimationFrame(draw);
})();
""".Replace(
    "__PUBLISHERSTUDIO_BLOB_RUNTIME_PAYLOAD__",
    payload,
    StringComparison.Ordinal);
}
