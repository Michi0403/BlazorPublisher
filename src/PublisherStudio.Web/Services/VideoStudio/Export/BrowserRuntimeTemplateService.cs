namespace PublisherStudio.Services.VideoStudio.Export;

/// <summary>
/// Defines the contract for browser runtime template behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IBrowserRuntimeTemplateService
{
    /// <summary>
    /// Creates blob runtime as part of the browser runtime template service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="payload">Payload value supplied to the browser runtime template operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string CreateBlobRuntime(string payload);
}

/// <summary>
/// Coordinates browser runtime template behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class BrowserRuntimeTemplateService : IBrowserRuntimeTemplateService
{
    /// <summary>
    /// Creates blob runtime as part of the browser runtime template service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="payload">Payload value supplied to the browser runtime template operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string CreateBlobRuntime(string payload) {
    try
    {
        return """
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
  const pixels=(points,offsetX=0,offsetY=0)=>points.map(point=>({x:(.08+.84*point[0])*canvas.width+offsetX,y:(.08+.84*point[1])*canvas.height+offsetY}));
  const pathPixels=points=>{ctx.beginPath();points.forEach((point,index)=>{if(index===0)ctx.moveTo(point.x,point.y);else ctx.lineTo(point.x,point.y);});ctx.closePath();};
  const path=(points,offsetX=0,offsetY=0)=>pathPixels(pixels(points,offsetX,offsetY));
  const draw=time=>{
    resize();ctx.clearRect(0,0,canvas.width,canvas.height);
    const phase=config.animate&&config.morphEnabled?(Math.sin(time*.001*config.speed*Math.PI)+1)/2:clamp(config.morphAmount,0,1);
    const points=source.map((point,index)=>[point[0]+(target[index][0]-point[0])*phase,point[1]+(target[index][1]-point[1])*phase]);
    const depth=Math.max(8,Math.round(Math.min(canvas.width,canvas.height)*clamp(config.depth,.02,.5)*.48));
    const spin=config.animate?Math.sin(time*.00055*Math.max(.2,config.speed||1))*.18:0;
    const angle=.70+spin,offsetX=Math.cos(angle)*depth,offsetY=Math.sin(angle)*depth;
    const front=pixels(points),back=pixels(points,offsetX,offsetY);
    ctx.save();ctx.shadowColor='rgba(2,6,23,.62)';ctx.shadowBlur=Math.max(4,depth*.28);ctx.shadowOffsetX=offsetX*.18;ctx.shadowOffsetY=offsetY*.18;
    pathPixels(back);const cap=ctx.createLinearGradient(0,0,offsetX+canvas.width,offsetY+canvas.height);cap.addColorStop(0,'rgba(30,64,175,.78)');cap.addColorStop(1,'rgba(2,6,23,.96)');ctx.fillStyle=cap;ctx.fill();ctx.restore();
    for(let index=0;index<front.length;index++){
      const next=(index+1)%front.length,a=front[index],b=front[next],c=back[next],d=back[index];
      const ex=b.x-a.x,ey=b.y-a.y,len=Math.max(1,Math.hypot(ex,ey)),nx=ey/len,ny=-ex/len,depthLen=Math.max(1,Math.hypot(offsetX,offsetY));
      const light=clamp(.52+(nx*-offsetX+ny*-offsetY)/depthLen*.28,.16,.86);
      ctx.beginPath();ctx.moveTo(a.x,a.y);ctx.lineTo(b.x,b.y);ctx.lineTo(c.x,c.y);ctx.lineTo(d.x,d.y);ctx.closePath();
      const side=ctx.createLinearGradient(a.x,a.y,d.x,d.y);side.addColorStop(0,`rgba(${Math.round(36+70*light)},${Math.round(82+95*light)},${Math.round(132+100*light)},.96)`);side.addColorStop(1,`rgba(${Math.round(4+20*light)},${Math.round(15+35*light)},${Math.round(40+55*light)},.99)`);ctx.fillStyle=side;ctx.fill();ctx.strokeStyle='rgba(186,230,253,.30)';ctx.lineWidth=Math.max(1,depth*.025);ctx.stroke();
    }
    path(points);const gradient=ctx.createLinearGradient(0,0,canvas.width,canvas.height);gradient.addColorStop(0,'rgba(125,211,252,.98)');gradient.addColorStop(.48,'rgba(14,165,233,.92)');gradient.addColorStop(1,'rgba(30,64,175,.96)');ctx.globalAlpha=clamp(config.opacity,0,1);ctx.fillStyle=gradient;ctx.fill();ctx.save();ctx.clip();const shine=ctx.createRadialGradient(canvas.width*.32,canvas.height*.25,0,canvas.width*.32,canvas.height*.25,Math.max(canvas.width,canvas.height)*.7);shine.addColorStop(0,'rgba(255,255,255,.62)');shine.addColorStop(.35,'rgba(255,255,255,.08)');shine.addColorStop(1,'rgba(0,0,0,.34)');ctx.fillStyle=shine;ctx.fillRect(0,0,canvas.width,canvas.height);ctx.restore();ctx.globalAlpha=1;requestAnimationFrame(draw);
  };
  requestAnimationFrame(draw);
})();
""".Replace(
    "__PUBLISHERSTUDIO_BLOB_RUNTIME_PAYLOAD__",
    payload,
    StringComparison.Ordinal);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method BrowserRuntimeTemplateService.CreateBlobRuntime failed: {__serviceMethodException}");
        throw;
    }
}
}
