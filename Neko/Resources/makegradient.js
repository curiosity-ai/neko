var Ut=Object.defineProperty;var Bt=(R,k,L)=>k in R?Ut(R,k,{enumerable:!0,configurable:!0,writable:!0,value:L}):R[k]=L;var O=(R,k,L)=>Bt(R,typeof k!="symbol"?k+"":k,L);(function(){"use strict";function R(t){let e=t[0],i=t[1],n=t[2];return Math.sqrt(e*e+i*i+n*n)}function k(t,e){return t[0]=e[0],t[1]=e[1],t[2]=e[2],t}function L(t,e,i,n){return t[0]=e,t[1]=i,t[2]=n,t}function q(t,e,i){return t[0]=e[0]+i[0],t[1]=e[1]+i[1],t[2]=e[2]+i[2],t}function $(t,e,i){return t[0]=e[0]-i[0],t[1]=e[1]-i[1],t[2]=e[2]-i[2],t}function ce(t,e,i){return t[0]=e[0]*i[0],t[1]=e[1]*i[1],t[2]=e[2]*i[2],t}function fe(t,e,i){return t[0]=e[0]/i[0],t[1]=e[1]/i[1],t[2]=e[2]/i[2],t}function I(t,e,i){return t[0]=e[0]*i,t[1]=e[1]*i,t[2]=e[2]*i,t}function de(t,e){let i=e[0]-t[0],n=e[1]-t[1],s=e[2]-t[2];return Math.sqrt(i*i+n*n+s*s)}function ge(t,e){let i=e[0]-t[0],n=e[1]-t[1],s=e[2]-t[2];return i*i+n*n+s*s}function W(t){let e=t[0],i=t[1],n=t[2];return e*e+i*i+n*n}function pe(t,e){return t[0]=-e[0],t[1]=-e[1],t[2]=-e[2],t}function ue(t,e){return t[0]=1/e[0],t[1]=1/e[1],t[2]=1/e[2],t}function N(t,e){let i=e[0],n=e[1],s=e[2],r=i*i+n*n+s*s;return r>0&&(r=1/Math.sqrt(r)),t[0]=e[0]*r,t[1]=e[1]*r,t[2]=e[2]*r,t}function X(t,e){return t[0]*e[0]+t[1]*e[1]+t[2]*e[2]}function Y(t,e,i){let n=e[0],s=e[1],r=e[2],l=i[0],a=i[1],h=i[2];return t[0]=s*h-r*a,t[1]=r*l-n*h,t[2]=n*a-s*l,t}function me(t,e,i,n){let s=e[0],r=e[1],l=e[2];return t[0]=s+n*(i[0]-s),t[1]=r+n*(i[1]-r),t[2]=l+n*(i[2]-l),t}function ve(t,e,i,n,s){const r=Math.exp(-n*s);let l=e[0],a=e[1],h=e[2];return t[0]=i[0]+(l-i[0])*r,t[1]=i[1]+(a-i[1])*r,t[2]=i[2]+(h-i[2])*r,t}function xe(t,e,i){let n=e[0],s=e[1],r=e[2],l=i[3]*n+i[7]*s+i[11]*r+i[15];return l=l||1,t[0]=(i[0]*n+i[4]*s+i[8]*r+i[12])/l,t[1]=(i[1]*n+i[5]*s+i[9]*r+i[13])/l,t[2]=(i[2]*n+i[6]*s+i[10]*r+i[14])/l,t}function ye(t,e,i){let n=e[0],s=e[1],r=e[2],l=i[3]*n+i[7]*s+i[11]*r+i[15];return l=l||1,t[0]=(i[0]*n+i[4]*s+i[8]*r)/l,t[1]=(i[1]*n+i[5]*s+i[9]*r)/l,t[2]=(i[2]*n+i[6]*s+i[10]*r)/l,t}function we(t,e,i){let n=e[0],s=e[1],r=e[2];return t[0]=n*i[0]+s*i[3]+r*i[6],t[1]=n*i[1]+s*i[4]+r*i[7],t[2]=n*i[2]+s*i[5]+r*i[8],t}function be(t,e,i){let n=e[0],s=e[1],r=e[2],l=i[0],a=i[1],h=i[2],o=i[3],c=a*r-h*s,f=h*n-l*r,d=l*s-a*n,g=a*d-h*f,p=h*c-l*d,u=l*f-a*c,m=o*2;return c*=m,f*=m,d*=m,g*=2,p*=2,u*=2,t[0]=n+c+g,t[1]=s+f+p,t[2]=r+d+u,t}const Ce=function(){const t=[0,0,0],e=[0,0,0];return function(i,n){k(t,i),k(e,n),N(t,t),N(e,e);let s=X(t,e);return s>1?0:s<-1?Math.PI:Math.acos(s)}}();function Me(t,e){return t[0]===e[0]&&t[1]===e[1]&&t[2]===e[2]}class T extends Array{constructor(e=0,i=e,n=e){return super(e,i,n),this}get x(){return this[0]}get y(){return this[1]}get z(){return this[2]}set x(e){this[0]=e}set y(e){this[1]=e}set z(e){this[2]=e}set(e,i=e,n=e){return e.length?this.copy(e):(L(this,e,i,n),this)}copy(e){return k(this,e),this}add(e,i){return i?q(this,e,i):q(this,this,e),this}sub(e,i){return i?$(this,e,i):$(this,this,e),this}multiply(e){return e.length?ce(this,this,e):I(this,this,e),this}divide(e){return e.length?fe(this,this,e):I(this,this,1/e),this}inverse(e=this){return ue(this,e),this}len(){return R(this)}distance(e){return e?de(this,e):R(this)}squaredLen(){return W(this)}squaredDistance(e){return e?ge(this,e):W(this)}negate(e=this){return pe(this,e),this}cross(e,i){return i?Y(this,e,i):Y(this,this,e),this}scale(e){return I(this,this,e),this}normalize(){return N(this,this),this}dot(e){return X(this,e)}equals(e){return Me(this,e)}applyMatrix3(e){return we(this,this,e),this}applyMatrix4(e){return xe(this,this,e),this}scaleRotateMatrix4(e){return ye(this,this,e),this}applyQuaternion(e){return be(this,this,e),this}angle(e){return Ce(this,e)}lerp(e,i){return me(this,this,e,i),this}smoothLerp(e,i,n){return ve(this,this,e,i,n),this}clone(){return new T(this[0],this[1],this[2])}fromArray(e,i=0){return this[0]=e[i],this[1]=e[i+1],this[2]=e[i+2],this}toArray(e=[],i=0){return e[i]=this[0],e[i+1]=this[1],e[i+2]=this[2],e}transformDirection(e){const i=this[0],n=this[1],s=this[2];return this[0]=e[0]*i+e[4]*n+e[8]*s,this[1]=e[1]*i+e[5]*n+e[9]*s,this[2]=e[2]*i+e[6]*n+e[10]*s,this.normalize()}}const j=new T;let Se=1,Ae=1,H=!1;class Ee{constructor(e,i={}){e.canvas||console.error("gl not passed as first argument to Geometry"),this.gl=e,this.attributes=i,this.id=Se++,this.VAOs={},this.drawRange={start:0,count:0},this.instancedCount=0,this.gl.renderer.bindVertexArray(null),this.gl.renderer.currentGeometry=null,this.glState=this.gl.renderer.state;for(let n in i)this.addAttribute(n,i[n])}addAttribute(e,i){if(this.attributes[e]=i,i.id=Ae++,i.size=i.size||1,i.type=i.type||(i.data.constructor===Float32Array?this.gl.FLOAT:i.data.constructor===Uint16Array?this.gl.UNSIGNED_SHORT:this.gl.UNSIGNED_INT),i.target=e==="index"?this.gl.ELEMENT_ARRAY_BUFFER:this.gl.ARRAY_BUFFER,i.normalized=i.normalized||!1,i.stride=i.stride||0,i.offset=i.offset||0,i.count=i.count||(i.stride?i.data.byteLength/i.stride:i.data.length/i.size),i.divisor=i.instanced||0,i.needsUpdate=!1,i.usage=i.usage||this.gl.STATIC_DRAW,i.buffer||this.updateAttribute(i),i.divisor){if(this.isInstanced=!0,this.instancedCount&&this.instancedCount!==i.count*i.divisor)return console.warn("geometry has multiple instanced buffers of different length"),this.instancedCount=Math.min(this.instancedCount,i.count*i.divisor);this.instancedCount=i.count*i.divisor}else e==="index"?this.drawRange.count=i.count:this.attributes.index||(this.drawRange.count=Math.max(this.drawRange.count,i.count))}updateAttribute(e){const i=!e.buffer;i&&(e.buffer=this.gl.createBuffer()),this.glState.boundBuffer!==e.buffer&&(this.gl.bindBuffer(e.target,e.buffer),this.glState.boundBuffer=e.buffer),i?this.gl.bufferData(e.target,e.data,e.usage):this.gl.bufferSubData(e.target,0,e.data),e.needsUpdate=!1}setIndex(e){this.addAttribute("index",e)}setDrawRange(e,i){this.drawRange.start=e,this.drawRange.count=i}setInstancedCount(e){this.instancedCount=e}createVAO(e){this.VAOs[e.attributeOrder]=this.gl.renderer.createVertexArray(),this.gl.renderer.bindVertexArray(this.VAOs[e.attributeOrder]),this.bindAttributes(e)}bindAttributes(e){e.attributeLocations.forEach((i,{name:n,type:s})=>{if(!this.attributes[n]){console.warn(`active attribute ${n} not being supplied`);return}const r=this.attributes[n];this.gl.bindBuffer(r.target,r.buffer),this.glState.boundBuffer=r.buffer;let l=1;s===35674&&(l=2),s===35675&&(l=3),s===35676&&(l=4);const a=r.size/l,h=l===1?0:l*l*4,o=l===1?0:l*4;for(let c=0;c<l;c++)this.gl.vertexAttribPointer(i+c,a,r.type,r.normalized,r.stride+h,r.offset+c*o),this.gl.enableVertexAttribArray(i+c),this.gl.renderer.vertexAttribDivisor(i+c,r.divisor)}),this.attributes.index&&this.gl.bindBuffer(this.gl.ELEMENT_ARRAY_BUFFER,this.attributes.index.buffer)}draw({program:e,mode:i=this.gl.TRIANGLES}){var s;this.gl.renderer.currentGeometry!==`${this.id}_${e.attributeOrder}`&&(this.VAOs[e.attributeOrder]||this.createVAO(e),this.gl.renderer.bindVertexArray(this.VAOs[e.attributeOrder]),this.gl.renderer.currentGeometry=`${this.id}_${e.attributeOrder}`),e.attributeLocations.forEach((r,{name:l})=>{const a=this.attributes[l];a.needsUpdate&&this.updateAttribute(a)});let n=2;((s=this.attributes.index)==null?void 0:s.type)===this.gl.UNSIGNED_INT&&(n=4),this.isInstanced?this.attributes.index?this.gl.renderer.drawElementsInstanced(i,this.drawRange.count,this.attributes.index.type,this.attributes.index.offset+this.drawRange.start*n,this.instancedCount):this.gl.renderer.drawArraysInstanced(i,this.drawRange.start,this.drawRange.count,this.instancedCount):this.attributes.index?this.gl.drawElements(i,this.drawRange.count,this.attributes.index.type,this.attributes.index.offset+this.drawRange.start*n):this.gl.drawArrays(i,this.drawRange.start,this.drawRange.count)}getPosition(){const e=this.attributes.position;if(e.data)return e;if(!H)return console.warn("No position buffer data found to compute bounds"),H=!0}computeBoundingBox(e){e||(e=this.getPosition());const i=e.data,n=e.size;this.bounds||(this.bounds={min:new T,max:new T,center:new T,scale:new T,radius:1/0});const s=this.bounds.min,r=this.bounds.max,l=this.bounds.center,a=this.bounds.scale;s.set(1/0),r.set(-1/0);for(let h=0,o=i.length;h<o;h+=n){const c=i[h],f=i[h+1],d=i[h+2];s.x=Math.min(c,s.x),s.y=Math.min(f,s.y),s.z=Math.min(d,s.z),r.x=Math.max(c,r.x),r.y=Math.max(f,r.y),r.z=Math.max(d,r.z)}a.sub(r,s),l.add(s,r).divide(2)}computeBoundingSphere(e){e||(e=this.getPosition());const i=e.data,n=e.size;this.bounds||this.computeBoundingBox(e);let s=0;for(let r=0,l=i.length;r<l;r+=n)j.fromArray(i,r),s=Math.max(s,this.bounds.center.squaredDistance(j));this.bounds.radius=Math.sqrt(s)}remove(){for(let e in this.VAOs)this.gl.renderer.deleteVertexArray(this.VAOs[e]),delete this.VAOs[e];for(let e in this.attributes)this.gl.deleteBuffer(this.attributes[e].buffer),delete this.attributes[e]}}let _e=1;const Z={};class ze{constructor(e,{vertex:i,fragment:n,uniforms:s={},transparent:r=!1,cullFace:l=e.BACK,frontFace:a=e.CCW,depthTest:h=!0,depthWrite:o=!0,depthFunc:c=e.LEQUAL}={}){e.canvas||console.error("gl not passed as first argument to Program"),this.gl=e,this.uniforms=s,this.id=_e++,i||console.warn("vertex shader not supplied"),n||console.warn("fragment shader not supplied"),this.transparent=r,this.cullFace=l,this.frontFace=a,this.depthTest=h,this.depthWrite=o,this.depthFunc=c,this.blendFunc={},this.blendEquation={},this.stencilFunc={},this.stencilOp={},this.transparent&&!this.blendFunc.src&&(this.gl.renderer.premultipliedAlpha?this.setBlendFunc(this.gl.ONE,this.gl.ONE_MINUS_SRC_ALPHA):this.setBlendFunc(this.gl.SRC_ALPHA,this.gl.ONE_MINUS_SRC_ALPHA)),this.vertexShader=e.createShader(e.VERTEX_SHADER),this.fragmentShader=e.createShader(e.FRAGMENT_SHADER),this.program=e.createProgram(),e.attachShader(this.program,this.vertexShader),e.attachShader(this.program,this.fragmentShader),this.setShaders({vertex:i,fragment:n})}setShaders({vertex:e,fragment:i}){if(e&&(this.gl.shaderSource(this.vertexShader,e),this.gl.compileShader(this.vertexShader),this.gl.getShaderInfoLog(this.vertexShader)!==""&&console.warn(`${this.gl.getShaderInfoLog(this.vertexShader)}
Vertex Shader
${Q(e)}`)),i&&(this.gl.shaderSource(this.fragmentShader,i),this.gl.compileShader(this.fragmentShader),this.gl.getShaderInfoLog(this.fragmentShader)!==""&&console.warn(`${this.gl.getShaderInfoLog(this.fragmentShader)}
Fragment Shader
${Q(i)}`)),this.gl.linkProgram(this.program),!this.gl.getProgramParameter(this.program,this.gl.LINK_STATUS))return console.warn(this.gl.getProgramInfoLog(this.program));this.uniformLocations=new Map;let n=this.gl.getProgramParameter(this.program,this.gl.ACTIVE_UNIFORMS);for(let l=0;l<n;l++){let a=this.gl.getActiveUniform(this.program,l);this.uniformLocations.set(a,this.gl.getUniformLocation(this.program,a.name));const h=a.name.match(/(\w+)/g);a.uniformName=h[0],a.nameComponents=h.slice(1)}this.attributeLocations=new Map;const s=[],r=this.gl.getProgramParameter(this.program,this.gl.ACTIVE_ATTRIBUTES);for(let l=0;l<r;l++){const a=this.gl.getActiveAttrib(this.program,l),h=this.gl.getAttribLocation(this.program,a.name);h!==-1&&(s[h]=a.name,this.attributeLocations.set(a,h))}this.attributeOrder=s.join("")}setBlendFunc(e,i,n,s){this.blendFunc.src=e,this.blendFunc.dst=i,this.blendFunc.srcAlpha=n,this.blendFunc.dstAlpha=s,e&&(this.transparent=!0)}setBlendEquation(e,i){this.blendEquation.modeRGB=e,this.blendEquation.modeAlpha=i}setStencilFunc(e,i,n){this.stencilRef=i,this.stencilFunc.func=e,this.stencilFunc.ref=i,this.stencilFunc.mask=n}setStencilOp(e,i,n){this.stencilOp.stencilFail=e,this.stencilOp.depthFail=i,this.stencilOp.depthPass=n}applyState(){this.depthTest?this.gl.renderer.enable(this.gl.DEPTH_TEST):this.gl.renderer.disable(this.gl.DEPTH_TEST),this.cullFace?this.gl.renderer.enable(this.gl.CULL_FACE):this.gl.renderer.disable(this.gl.CULL_FACE),this.blendFunc.src?this.gl.renderer.enable(this.gl.BLEND):this.gl.renderer.disable(this.gl.BLEND),this.cullFace&&this.gl.renderer.setCullFace(this.cullFace),this.gl.renderer.setFrontFace(this.frontFace),this.gl.renderer.setDepthMask(this.depthWrite),this.gl.renderer.setDepthFunc(this.depthFunc),this.blendFunc.src&&this.gl.renderer.setBlendFunc(this.blendFunc.src,this.blendFunc.dst,this.blendFunc.srcAlpha,this.blendFunc.dstAlpha),this.gl.renderer.setBlendEquation(this.blendEquation.modeRGB,this.blendEquation.modeAlpha),this.stencilFunc.func||this.stencilOp.stencilFail?this.gl.renderer.enable(this.gl.STENCIL_TEST):this.gl.renderer.disable(this.gl.STENCIL_TEST),this.gl.renderer.setStencilFunc(this.stencilFunc.func,this.stencilFunc.ref,this.stencilFunc.mask),this.gl.renderer.setStencilOp(this.stencilOp.stencilFail,this.stencilOp.depthFail,this.stencilOp.depthPass)}use({flipFaces:e=!1}={}){let i=-1;this.gl.renderer.state.currentProgram===this.id||(this.gl.useProgram(this.program),this.gl.renderer.state.currentProgram=this.id),this.uniformLocations.forEach((s,r)=>{let l=this.uniforms[r.uniformName];for(const a of r.nameComponents){if(!l)break;if(a in l)l=l[a];else{if(Array.isArray(l.value))break;l=void 0;break}}if(!l)return K(`Active uniform ${r.name} has not been supplied`);if(l&&l.value===void 0)return K(`${r.name} uniform is missing a value parameter`);if(l.value.texture)return i=i+1,l.value.update(i),P(this.gl,r.type,s,i);if(l.value.length&&l.value[0].texture){const a=[];return l.value.forEach(h=>{i=i+1,h.update(i),a.push(i)}),P(this.gl,r.type,s,a)}P(this.gl,r.type,s,l.value)}),this.applyState(),e&&this.gl.renderer.setFrontFace(this.frontFace===this.gl.CCW?this.gl.CW:this.gl.CCW)}remove(){this.gl.deleteProgram(this.program)}}function P(t,e,i,n){n=n.length?Fe(n):n;const s=t.renderer.state.uniformLocations.get(i);if(n.length)if(s===void 0||s.length!==n.length)t.renderer.state.uniformLocations.set(i,n.slice(0));else{if(De(s,n))return;s.set?s.set(n):Te(s,n),t.renderer.state.uniformLocations.set(i,s)}else{if(s===n)return;t.renderer.state.uniformLocations.set(i,n)}switch(e){case 5126:return n.length?t.uniform1fv(i,n):t.uniform1f(i,n);case 35664:return t.uniform2fv(i,n);case 35665:return t.uniform3fv(i,n);case 35666:return t.uniform4fv(i,n);case 35670:case 5124:case 35678:case 36306:case 35680:case 36289:return n.length?t.uniform1iv(i,n):t.uniform1i(i,n);case 35671:case 35667:return t.uniform2iv(i,n);case 35672:case 35668:return t.uniform3iv(i,n);case 35673:case 35669:return t.uniform4iv(i,n);case 35674:return t.uniformMatrix2fv(i,!1,n);case 35675:return t.uniformMatrix3fv(i,!1,n);case 35676:return t.uniformMatrix4fv(i,!1,n)}}function Q(t){let e=t.split(`
`);for(let i=0;i<e.length;i++)e[i]=i+1+": "+e[i];return e.join(`
`)}function Fe(t){const e=t.length,i=t[0].length;if(i===void 0)return t;const n=e*i;let s=Z[n];s||(Z[n]=s=new Float32Array(n));for(let r=0;r<e;r++)s.set(t[r],r*i);return s}function De(t,e){if(t.length!==e.length)return!1;for(let i=0,n=t.length;i<n;i++)if(t[i]!==e[i])return!1;return!0}function Te(t,e){for(let i=0,n=t.length;i<n;i++)t[i]=e[i]}let G=0;function K(t){G>100||(console.warn(t),G++,G>100&&console.warn("More than 100 program warnings - stopping logs."))}const V=new T;let Re=1;class ke{constructor({canvas:e=document.createElement("canvas"),width:i=300,height:n=150,dpr:s=1,alpha:r=!1,depth:l=!0,stencil:a=!1,antialias:h=!1,premultipliedAlpha:o=!1,preserveDrawingBuffer:c=!1,powerPreference:f="default",autoClear:d=!0,webgl:g=2}={}){const p={alpha:r,depth:l,stencil:a,antialias:h,premultipliedAlpha:o,preserveDrawingBuffer:c,powerPreference:f};this.dpr=s,this.alpha=r,this.color=!0,this.depth=l,this.stencil=a,this.premultipliedAlpha=o,this.autoClear=d,this.id=Re++,g===2&&(this.gl=e.getContext("webgl2",p)),this.isWebgl2=!!this.gl,this.gl||(this.gl=e.getContext("webgl",p)),this.gl||console.error("unable to create webgl context"),this.gl.renderer=this,this.setSize(i,n),this.state={},this.state.blendFunc={src:this.gl.ONE,dst:this.gl.ZERO},this.state.blendEquation={modeRGB:this.gl.FUNC_ADD},this.state.cullFace=!1,this.state.frontFace=this.gl.CCW,this.state.depthMask=!0,this.state.depthFunc=this.gl.LEQUAL,this.state.premultiplyAlpha=!1,this.state.flipY=!1,this.state.unpackAlignment=4,this.state.framebuffer=null,this.state.viewport={x:0,y:0,width:null,height:null},this.state.textureUnits=[],this.state.activeTextureUnit=0,this.state.boundBuffer=null,this.state.uniformLocations=new Map,this.state.currentProgram=null,this.extensions={},this.isWebgl2?(this.getExtension("EXT_color_buffer_float"),this.getExtension("OES_texture_float_linear")):(this.getExtension("OES_texture_float"),this.getExtension("OES_texture_float_linear"),this.getExtension("OES_texture_half_float"),this.getExtension("OES_texture_half_float_linear"),this.getExtension("OES_element_index_uint"),this.getExtension("OES_standard_derivatives"),this.getExtension("EXT_sRGB"),this.getExtension("WEBGL_depth_texture"),this.getExtension("WEBGL_draw_buffers")),this.getExtension("WEBGL_compressed_texture_astc"),this.getExtension("EXT_texture_compression_bptc"),this.getExtension("WEBGL_compressed_texture_s3tc"),this.getExtension("WEBGL_compressed_texture_etc1"),this.getExtension("WEBGL_compressed_texture_pvrtc"),this.getExtension("WEBKIT_WEBGL_compressed_texture_pvrtc"),this.vertexAttribDivisor=this.getExtension("ANGLE_instanced_arrays","vertexAttribDivisor","vertexAttribDivisorANGLE"),this.drawArraysInstanced=this.getExtension("ANGLE_instanced_arrays","drawArraysInstanced","drawArraysInstancedANGLE"),this.drawElementsInstanced=this.getExtension("ANGLE_instanced_arrays","drawElementsInstanced","drawElementsInstancedANGLE"),this.createVertexArray=this.getExtension("OES_vertex_array_object","createVertexArray","createVertexArrayOES"),this.bindVertexArray=this.getExtension("OES_vertex_array_object","bindVertexArray","bindVertexArrayOES"),this.deleteVertexArray=this.getExtension("OES_vertex_array_object","deleteVertexArray","deleteVertexArrayOES"),this.drawBuffers=this.getExtension("WEBGL_draw_buffers","drawBuffers","drawBuffersWEBGL"),this.parameters={},this.parameters.maxTextureUnits=this.gl.getParameter(this.gl.MAX_COMBINED_TEXTURE_IMAGE_UNITS),this.parameters.maxAnisotropy=this.getExtension("EXT_texture_filter_anisotropic")?this.gl.getParameter(this.getExtension("EXT_texture_filter_anisotropic").MAX_TEXTURE_MAX_ANISOTROPY_EXT):0}setSize(e,i){this.width=e,this.height=i,this.gl.canvas.width=e*this.dpr,this.gl.canvas.height=i*this.dpr,this.gl.canvas.style&&Object.assign(this.gl.canvas.style,{width:e+"px",height:i+"px"})}setViewport(e,i,n=0,s=0){this.state.viewport.width===e&&this.state.viewport.height===i||(this.state.viewport.width=e,this.state.viewport.height=i,this.state.viewport.x=n,this.state.viewport.y=s,this.gl.viewport(n,s,e,i))}setScissor(e,i,n=0,s=0){this.gl.scissor(n,s,e,i)}enable(e){this.state[e]!==!0&&(this.gl.enable(e),this.state[e]=!0)}disable(e){this.state[e]!==!1&&(this.gl.disable(e),this.state[e]=!1)}setBlendFunc(e,i,n,s){this.state.blendFunc.src===e&&this.state.blendFunc.dst===i&&this.state.blendFunc.srcAlpha===n&&this.state.blendFunc.dstAlpha===s||(this.state.blendFunc.src=e,this.state.blendFunc.dst=i,this.state.blendFunc.srcAlpha=n,this.state.blendFunc.dstAlpha=s,n!==void 0?this.gl.blendFuncSeparate(e,i,n,s):this.gl.blendFunc(e,i))}setBlendEquation(e,i){e=e||this.gl.FUNC_ADD,!(this.state.blendEquation.modeRGB===e&&this.state.blendEquation.modeAlpha===i)&&(this.state.blendEquation.modeRGB=e,this.state.blendEquation.modeAlpha=i,i!==void 0?this.gl.blendEquationSeparate(e,i):this.gl.blendEquation(e))}setCullFace(e){this.state.cullFace!==e&&(this.state.cullFace=e,this.gl.cullFace(e))}setFrontFace(e){this.state.frontFace!==e&&(this.state.frontFace=e,this.gl.frontFace(e))}setDepthMask(e){this.state.depthMask!==e&&(this.state.depthMask=e,this.gl.depthMask(e))}setDepthFunc(e){this.state.depthFunc!==e&&(this.state.depthFunc=e,this.gl.depthFunc(e))}setStencilMask(e){this.state.stencilMask!==e&&(this.state.stencilMask=e,this.gl.stencilMask(e))}setStencilFunc(e,i,n){this.state.stencilFunc===e&&this.state.stencilRef===i&&this.state.stencilFuncMask===n||(this.state.stencilFunc=e||this.gl.ALWAYS,this.state.stencilRef=i||0,this.state.stencilFuncMask=n||0,this.gl.stencilFunc(e||this.gl.ALWAYS,i||0,n||0))}setStencilOp(e,i,n){this.state.stencilFail===e&&this.state.stencilDepthFail===i&&this.state.stencilDepthPass===n||(this.state.stencilFail=e,this.state.stencilDepthFail=i,this.state.stencilDepthPass=n,this.gl.stencilOp(e,i,n))}activeTexture(e){this.state.activeTextureUnit!==e&&(this.state.activeTextureUnit=e,this.gl.activeTexture(this.gl.TEXTURE0+e))}bindFramebuffer({target:e=this.gl.FRAMEBUFFER,buffer:i=null}={}){this.state.framebuffer!==i&&(this.state.framebuffer=i,this.gl.bindFramebuffer(e,i))}getExtension(e,i,n){return i&&this.gl[i]?this.gl[i].bind(this.gl):(this.extensions[e]||(this.extensions[e]=this.gl.getExtension(e)),i?this.extensions[e]?this.extensions[e][n].bind(this.extensions[e]):null:this.extensions[e])}sortOpaque(e,i){return e.renderOrder!==i.renderOrder?e.renderOrder-i.renderOrder:e.program.id!==i.program.id?e.program.id-i.program.id:e.zDepth!==i.zDepth?e.zDepth-i.zDepth:i.id-e.id}sortTransparent(e,i){return e.renderOrder!==i.renderOrder?e.renderOrder-i.renderOrder:e.zDepth!==i.zDepth?i.zDepth-e.zDepth:i.id-e.id}sortUI(e,i){return e.renderOrder!==i.renderOrder?e.renderOrder-i.renderOrder:e.program.id!==i.program.id?e.program.id-i.program.id:i.id-e.id}getRenderList({scene:e,camera:i,frustumCull:n,sort:s}){let r=[];if(i&&n&&i.updateFrustum(),e.traverse(l=>{if(!l.visible)return!0;l.draw&&(n&&l.frustumCulled&&i&&!i.frustumIntersectsMesh(l)||r.push(l))}),s){const l=[],a=[],h=[];r.forEach(o=>{o.program.transparent?o.program.depthTest?a.push(o):h.push(o):l.push(o),o.zDepth=0,!(o.renderOrder!==0||!o.program.depthTest||!i)&&(o.worldMatrix.getTranslation(V),V.applyMatrix4(i.projectionViewMatrix),o.zDepth=V.z)}),l.sort(this.sortOpaque),a.sort(this.sortTransparent),h.sort(this.sortUI),r=l.concat(a,h)}return r}render({scene:e,camera:i,target:n=null,update:s=!0,sort:r=!0,frustumCull:l=!0,clear:a}){n===null?(this.bindFramebuffer(),this.setViewport(this.width*this.dpr,this.height*this.dpr)):(this.bindFramebuffer(n),this.setViewport(n.width,n.height)),(a||this.autoClear&&a!==!1)&&(this.depth&&(!n||n.depth)&&(this.enable(this.gl.DEPTH_TEST),this.setDepthMask(!0)),(this.stencil||!n||n.stencil)&&(this.enable(this.gl.STENCIL_TEST),this.setStencilMask(255)),this.gl.clear((this.color?this.gl.COLOR_BUFFER_BIT:0)|(this.depth?this.gl.DEPTH_BUFFER_BIT:0)|(this.stencil?this.gl.STENCIL_BUFFER_BIT:0))),s&&e.updateMatrixWorld(),i&&i.updateMatrixWorld(),this.getRenderList({scene:e,camera:i,frustumCull:l,sort:r}).forEach(o=>{o.draw({camera:i})})}}function Oe(t,e){return t[0]=e[0],t[1]=e[1],t[2]=e[2],t[3]=e[3],t}function Le(t,e,i,n,s){return t[0]=e,t[1]=i,t[2]=n,t[3]=s,t}function Ue(t,e){let i=e[0],n=e[1],s=e[2],r=e[3],l=i*i+n*n+s*s+r*r;return l>0&&(l=1/Math.sqrt(l)),t[0]=i*l,t[1]=n*l,t[2]=s*l,t[3]=r*l,t}function Be(t,e){return t[0]*e[0]+t[1]*e[1]+t[2]*e[2]+t[3]*e[3]}function Ie(t){return t[0]=0,t[1]=0,t[2]=0,t[3]=1,t}function Ne(t,e,i){i=i*.5;let n=Math.sin(i);return t[0]=n*e[0],t[1]=n*e[1],t[2]=n*e[2],t[3]=Math.cos(i),t}function J(t,e,i){let n=e[0],s=e[1],r=e[2],l=e[3],a=i[0],h=i[1],o=i[2],c=i[3];return t[0]=n*c+l*a+s*o-r*h,t[1]=s*c+l*h+r*a-n*o,t[2]=r*c+l*o+n*h-s*a,t[3]=l*c-n*a-s*h-r*o,t}function Pe(t,e,i){i*=.5;let n=e[0],s=e[1],r=e[2],l=e[3],a=Math.sin(i),h=Math.cos(i);return t[0]=n*h+l*a,t[1]=s*h+r*a,t[2]=r*h-s*a,t[3]=l*h-n*a,t}function Ge(t,e,i){i*=.5;let n=e[0],s=e[1],r=e[2],l=e[3],a=Math.sin(i),h=Math.cos(i);return t[0]=n*h-r*a,t[1]=s*h+l*a,t[2]=r*h+n*a,t[3]=l*h-s*a,t}function Ve(t,e,i){i*=.5;let n=e[0],s=e[1],r=e[2],l=e[3],a=Math.sin(i),h=Math.cos(i);return t[0]=n*h+s*a,t[1]=s*h-n*a,t[2]=r*h+l*a,t[3]=l*h-r*a,t}function qe(t,e,i,n){let s=e[0],r=e[1],l=e[2],a=e[3],h=i[0],o=i[1],c=i[2],f=i[3],d,g,p,u,m;return g=s*h+r*o+l*c+a*f,g<0&&(g=-g,h=-h,o=-o,c=-c,f=-f),1-g>1e-6?(d=Math.acos(g),p=Math.sin(d),u=Math.sin((1-n)*d)/p,m=Math.sin(n*d)/p):(u=1-n,m=n),t[0]=u*s+m*h,t[1]=u*r+m*o,t[2]=u*l+m*c,t[3]=u*a+m*f,t}function $e(t,e){let i=e[0],n=e[1],s=e[2],r=e[3],l=i*i+n*n+s*s+r*r,a=l?1/l:0;return t[0]=-i*a,t[1]=-n*a,t[2]=-s*a,t[3]=r*a,t}function We(t,e){return t[0]=-e[0],t[1]=-e[1],t[2]=-e[2],t[3]=e[3],t}function Xe(t,e){let i=e[0]+e[4]+e[8],n;if(i>0)n=Math.sqrt(i+1),t[3]=.5*n,n=.5/n,t[0]=(e[5]-e[7])*n,t[1]=(e[6]-e[2])*n,t[2]=(e[1]-e[3])*n;else{let s=0;e[4]>e[0]&&(s=1),e[8]>e[s*3+s]&&(s=2);let r=(s+1)%3,l=(s+2)%3;n=Math.sqrt(e[s*3+s]-e[r*3+r]-e[l*3+l]+1),t[s]=.5*n,n=.5/n,t[3]=(e[r*3+l]-e[l*3+r])*n,t[r]=(e[r*3+s]+e[s*3+r])*n,t[l]=(e[l*3+s]+e[s*3+l])*n}return t}function Ye(t,e,i="YXZ"){let n=Math.sin(e[0]*.5),s=Math.cos(e[0]*.5),r=Math.sin(e[1]*.5),l=Math.cos(e[1]*.5),a=Math.sin(e[2]*.5),h=Math.cos(e[2]*.5);return i==="XYZ"?(t[0]=n*l*h+s*r*a,t[1]=s*r*h-n*l*a,t[2]=s*l*a+n*r*h,t[3]=s*l*h-n*r*a):i==="YXZ"?(t[0]=n*l*h+s*r*a,t[1]=s*r*h-n*l*a,t[2]=s*l*a-n*r*h,t[3]=s*l*h+n*r*a):i==="ZXY"?(t[0]=n*l*h-s*r*a,t[1]=s*r*h+n*l*a,t[2]=s*l*a+n*r*h,t[3]=s*l*h-n*r*a):i==="ZYX"?(t[0]=n*l*h-s*r*a,t[1]=s*r*h+n*l*a,t[2]=s*l*a-n*r*h,t[3]=s*l*h+n*r*a):i==="YZX"?(t[0]=n*l*h+s*r*a,t[1]=s*r*h+n*l*a,t[2]=s*l*a-n*r*h,t[3]=s*l*h-n*r*a):i==="XZY"&&(t[0]=n*l*h-s*r*a,t[1]=s*r*h-n*l*a,t[2]=s*l*a+n*r*h,t[3]=s*l*h+n*r*a),t}const je=Oe,He=Le,Ze=Be,Qe=Ue;class Ke extends Array{constructor(e=0,i=0,n=0,s=1){super(e,i,n,s),this.onChange=()=>{},this._target=this;const r=["0","1","2","3"];return new Proxy(this,{set(l,a){const h=Reflect.set(...arguments);return h&&r.includes(a)&&l.onChange(),h}})}get x(){return this[0]}get y(){return this[1]}get z(){return this[2]}get w(){return this[3]}set x(e){this._target[0]=e,this.onChange()}set y(e){this._target[1]=e,this.onChange()}set z(e){this._target[2]=e,this.onChange()}set w(e){this._target[3]=e,this.onChange()}identity(){return Ie(this._target),this.onChange(),this}set(e,i,n,s){return e.length?this.copy(e):(He(this._target,e,i,n,s),this.onChange(),this)}rotateX(e){return Pe(this._target,this._target,e),this.onChange(),this}rotateY(e){return Ge(this._target,this._target,e),this.onChange(),this}rotateZ(e){return Ve(this._target,this._target,e),this.onChange(),this}inverse(e=this._target){return $e(this._target,e),this.onChange(),this}conjugate(e=this._target){return We(this._target,e),this.onChange(),this}copy(e){return je(this._target,e),this.onChange(),this}normalize(e=this._target){return Qe(this._target,e),this.onChange(),this}multiply(e,i){return i?J(this._target,e,i):J(this._target,this._target,e),this.onChange(),this}dot(e){return Ze(this._target,e)}fromMatrix3(e){return Xe(this._target,e),this.onChange(),this}fromEuler(e,i){return Ye(this._target,e,e.order),i||this.onChange(),this}fromAxisAngle(e,i){return Ne(this._target,e,i),this.onChange(),this}slerp(e,i){return qe(this._target,this._target,e,i),this.onChange(),this}fromArray(e,i=0){return this._target[0]=e[i],this._target[1]=e[i+1],this._target[2]=e[i+2],this._target[3]=e[i+3],this.onChange(),this}toArray(e=[],i=0){return e[i]=this[0],e[i+1]=this[1],e[i+2]=this[2],e[i+3]=this[3],e}}const Je=1e-6;function et(t,e){return t[0]=e[0],t[1]=e[1],t[2]=e[2],t[3]=e[3],t[4]=e[4],t[5]=e[5],t[6]=e[6],t[7]=e[7],t[8]=e[8],t[9]=e[9],t[10]=e[10],t[11]=e[11],t[12]=e[12],t[13]=e[13],t[14]=e[14],t[15]=e[15],t}function tt(t,e,i,n,s,r,l,a,h,o,c,f,d,g,p,u,m){return t[0]=e,t[1]=i,t[2]=n,t[3]=s,t[4]=r,t[5]=l,t[6]=a,t[7]=h,t[8]=o,t[9]=c,t[10]=f,t[11]=d,t[12]=g,t[13]=p,t[14]=u,t[15]=m,t}function it(t){return t[0]=1,t[1]=0,t[2]=0,t[3]=0,t[4]=0,t[5]=1,t[6]=0,t[7]=0,t[8]=0,t[9]=0,t[10]=1,t[11]=0,t[12]=0,t[13]=0,t[14]=0,t[15]=1,t}function nt(t,e){let i=e[0],n=e[1],s=e[2],r=e[3],l=e[4],a=e[5],h=e[6],o=e[7],c=e[8],f=e[9],d=e[10],g=e[11],p=e[12],u=e[13],m=e[14],v=e[15],M=i*a-n*l,y=i*h-s*l,x=i*o-r*l,b=n*h-s*a,w=n*o-r*a,S=s*o-r*h,_=c*u-f*p,z=c*m-d*p,A=c*v-g*p,F=f*m-d*u,E=f*v-g*u,D=d*v-g*m,C=M*D-y*E+x*F+b*A-w*z+S*_;return C?(C=1/C,t[0]=(a*D-h*E+o*F)*C,t[1]=(s*E-n*D-r*F)*C,t[2]=(u*S-m*w+v*b)*C,t[3]=(d*w-f*S-g*b)*C,t[4]=(h*A-l*D-o*z)*C,t[5]=(i*D-s*A+r*z)*C,t[6]=(m*x-p*S-v*y)*C,t[7]=(c*S-d*x+g*y)*C,t[8]=(l*E-a*A+o*_)*C,t[9]=(n*A-i*E-r*_)*C,t[10]=(p*w-u*x+v*M)*C,t[11]=(f*x-c*w-g*M)*C,t[12]=(a*z-l*F-h*_)*C,t[13]=(i*F-n*z+s*_)*C,t[14]=(u*y-p*b-m*M)*C,t[15]=(c*b-f*y+d*M)*C,t):null}function ee(t){let e=t[0],i=t[1],n=t[2],s=t[3],r=t[4],l=t[5],a=t[6],h=t[7],o=t[8],c=t[9],f=t[10],d=t[11],g=t[12],p=t[13],u=t[14],m=t[15],v=e*l-i*r,M=e*a-n*r,y=e*h-s*r,x=i*a-n*l,b=i*h-s*l,w=n*h-s*a,S=o*p-c*g,_=o*u-f*g,z=o*m-d*g,A=c*u-f*p,F=c*m-d*p,E=f*m-d*u;return v*E-M*F+y*A+x*z-b*_+w*S}function te(t,e,i){let n=e[0],s=e[1],r=e[2],l=e[3],a=e[4],h=e[5],o=e[6],c=e[7],f=e[8],d=e[9],g=e[10],p=e[11],u=e[12],m=e[13],v=e[14],M=e[15],y=i[0],x=i[1],b=i[2],w=i[3];return t[0]=y*n+x*a+b*f+w*u,t[1]=y*s+x*h+b*d+w*m,t[2]=y*r+x*o+b*g+w*v,t[3]=y*l+x*c+b*p+w*M,y=i[4],x=i[5],b=i[6],w=i[7],t[4]=y*n+x*a+b*f+w*u,t[5]=y*s+x*h+b*d+w*m,t[6]=y*r+x*o+b*g+w*v,t[7]=y*l+x*c+b*p+w*M,y=i[8],x=i[9],b=i[10],w=i[11],t[8]=y*n+x*a+b*f+w*u,t[9]=y*s+x*h+b*d+w*m,t[10]=y*r+x*o+b*g+w*v,t[11]=y*l+x*c+b*p+w*M,y=i[12],x=i[13],b=i[14],w=i[15],t[12]=y*n+x*a+b*f+w*u,t[13]=y*s+x*h+b*d+w*m,t[14]=y*r+x*o+b*g+w*v,t[15]=y*l+x*c+b*p+w*M,t}function st(t,e,i){let n=i[0],s=i[1],r=i[2],l,a,h,o,c,f,d,g,p,u,m,v;return e===t?(t[12]=e[0]*n+e[4]*s+e[8]*r+e[12],t[13]=e[1]*n+e[5]*s+e[9]*r+e[13],t[14]=e[2]*n+e[6]*s+e[10]*r+e[14],t[15]=e[3]*n+e[7]*s+e[11]*r+e[15]):(l=e[0],a=e[1],h=e[2],o=e[3],c=e[4],f=e[5],d=e[6],g=e[7],p=e[8],u=e[9],m=e[10],v=e[11],t[0]=l,t[1]=a,t[2]=h,t[3]=o,t[4]=c,t[5]=f,t[6]=d,t[7]=g,t[8]=p,t[9]=u,t[10]=m,t[11]=v,t[12]=l*n+c*s+p*r+e[12],t[13]=a*n+f*s+u*r+e[13],t[14]=h*n+d*s+m*r+e[14],t[15]=o*n+g*s+v*r+e[15]),t}function rt(t,e,i){let n=i[0],s=i[1],r=i[2];return t[0]=e[0]*n,t[1]=e[1]*n,t[2]=e[2]*n,t[3]=e[3]*n,t[4]=e[4]*s,t[5]=e[5]*s,t[6]=e[6]*s,t[7]=e[7]*s,t[8]=e[8]*r,t[9]=e[9]*r,t[10]=e[10]*r,t[11]=e[11]*r,t[12]=e[12],t[13]=e[13],t[14]=e[14],t[15]=e[15],t}function lt(t,e,i,n){let s=n[0],r=n[1],l=n[2],a=Math.hypot(s,r,l),h,o,c,f,d,g,p,u,m,v,M,y,x,b,w,S,_,z,A,F,E,D,C,U;return Math.abs(a)<Je?null:(a=1/a,s*=a,r*=a,l*=a,h=Math.sin(i),o=Math.cos(i),c=1-o,f=e[0],d=e[1],g=e[2],p=e[3],u=e[4],m=e[5],v=e[6],M=e[7],y=e[8],x=e[9],b=e[10],w=e[11],S=s*s*c+o,_=r*s*c+l*h,z=l*s*c-r*h,A=s*r*c-l*h,F=r*r*c+o,E=l*r*c+s*h,D=s*l*c+r*h,C=r*l*c-s*h,U=l*l*c+o,t[0]=f*S+u*_+y*z,t[1]=d*S+m*_+x*z,t[2]=g*S+v*_+b*z,t[3]=p*S+M*_+w*z,t[4]=f*A+u*F+y*E,t[5]=d*A+m*F+x*E,t[6]=g*A+v*F+b*E,t[7]=p*A+M*F+w*E,t[8]=f*D+u*C+y*U,t[9]=d*D+m*C+x*U,t[10]=g*D+v*C+b*U,t[11]=p*D+M*C+w*U,e!==t&&(t[12]=e[12],t[13]=e[13],t[14]=e[14],t[15]=e[15]),t)}function at(t,e){return t[0]=e[12],t[1]=e[13],t[2]=e[14],t}function ie(t,e){let i=e[0],n=e[1],s=e[2],r=e[4],l=e[5],a=e[6],h=e[8],o=e[9],c=e[10];return t[0]=Math.hypot(i,n,s),t[1]=Math.hypot(r,l,a),t[2]=Math.hypot(h,o,c),t}function ht(t){let e=t[0],i=t[1],n=t[2],s=t[4],r=t[5],l=t[6],a=t[8],h=t[9],o=t[10];const c=e*e+i*i+n*n,f=s*s+r*r+l*l,d=a*a+h*h+o*o;return Math.sqrt(Math.max(c,f,d))}const ne=function(){const t=[1,1,1];return function(e,i){let n=t;ie(n,i);let s=1/n[0],r=1/n[1],l=1/n[2],a=i[0]*s,h=i[1]*r,o=i[2]*l,c=i[4]*s,f=i[5]*r,d=i[6]*l,g=i[8]*s,p=i[9]*r,u=i[10]*l,m=a+f+u,v=0;return m>0?(v=Math.sqrt(m+1)*2,e[3]=.25*v,e[0]=(d-p)/v,e[1]=(g-o)/v,e[2]=(h-c)/v):a>f&&a>u?(v=Math.sqrt(1+a-f-u)*2,e[3]=(d-p)/v,e[0]=.25*v,e[1]=(h+c)/v,e[2]=(g+o)/v):f>u?(v=Math.sqrt(1+f-a-u)*2,e[3]=(g-o)/v,e[0]=(h+c)/v,e[1]=.25*v,e[2]=(d+p)/v):(v=Math.sqrt(1+u-a-f)*2,e[3]=(h-c)/v,e[0]=(g+o)/v,e[1]=(d+p)/v,e[2]=.25*v),e}}();function ot(t,e,i,n){let s=R([t[0],t[1],t[2]]);const r=R([t[4],t[5],t[6]]),l=R([t[8],t[9],t[10]]);ee(t)<0&&(s=-s),i[0]=t[12],i[1]=t[13],i[2]=t[14];const h=t.slice(),o=1/s,c=1/r,f=1/l;h[0]*=o,h[1]*=o,h[2]*=o,h[4]*=c,h[5]*=c,h[6]*=c,h[8]*=f,h[9]*=f,h[10]*=f,ne(e,h),n[0]=s,n[1]=r,n[2]=l}function ct(t,e,i,n){const s=t,r=e[0],l=e[1],a=e[2],h=e[3],o=r+r,c=l+l,f=a+a,d=r*o,g=r*c,p=r*f,u=l*c,m=l*f,v=a*f,M=h*o,y=h*c,x=h*f,b=n[0],w=n[1],S=n[2];return s[0]=(1-(u+v))*b,s[1]=(g+x)*b,s[2]=(p-y)*b,s[3]=0,s[4]=(g-x)*w,s[5]=(1-(d+v))*w,s[6]=(m+M)*w,s[7]=0,s[8]=(p+y)*S,s[9]=(m-M)*S,s[10]=(1-(d+u))*S,s[11]=0,s[12]=i[0],s[13]=i[1],s[14]=i[2],s[15]=1,s}function ft(t,e){let i=e[0],n=e[1],s=e[2],r=e[3],l=i+i,a=n+n,h=s+s,o=i*l,c=n*l,f=n*a,d=s*l,g=s*a,p=s*h,u=r*l,m=r*a,v=r*h;return t[0]=1-f-p,t[1]=c+v,t[2]=d-m,t[3]=0,t[4]=c-v,t[5]=1-o-p,t[6]=g+u,t[7]=0,t[8]=d+m,t[9]=g-u,t[10]=1-o-f,t[11]=0,t[12]=0,t[13]=0,t[14]=0,t[15]=1,t}function dt(t,e,i,n,s){let r=1/Math.tan(e/2),l=1/(n-s);return t[0]=r/i,t[1]=0,t[2]=0,t[3]=0,t[4]=0,t[5]=r,t[6]=0,t[7]=0,t[8]=0,t[9]=0,t[10]=(s+n)*l,t[11]=-1,t[12]=0,t[13]=0,t[14]=2*s*n*l,t[15]=0,t}function gt(t,e,i,n,s,r,l){let a=1/(e-i),h=1/(n-s),o=1/(r-l);return t[0]=-2*a,t[1]=0,t[2]=0,t[3]=0,t[4]=0,t[5]=-2*h,t[6]=0,t[7]=0,t[8]=0,t[9]=0,t[10]=2*o,t[11]=0,t[12]=(e+i)*a,t[13]=(s+n)*h,t[14]=(l+r)*o,t[15]=1,t}function pt(t,e,i,n){let s=e[0],r=e[1],l=e[2],a=n[0],h=n[1],o=n[2],c=s-i[0],f=r-i[1],d=l-i[2],g=c*c+f*f+d*d;g===0?d=1:(g=1/Math.sqrt(g),c*=g,f*=g,d*=g);let p=h*d-o*f,u=o*c-a*d,m=a*f-h*c;return g=p*p+u*u+m*m,g===0&&(o?a+=1e-6:h?o+=1e-6:h+=1e-6,p=h*d-o*f,u=o*c-a*d,m=a*f-h*c,g=p*p+u*u+m*m),g=1/Math.sqrt(g),p*=g,u*=g,m*=g,t[0]=p,t[1]=u,t[2]=m,t[3]=0,t[4]=f*m-d*u,t[5]=d*p-c*m,t[6]=c*u-f*p,t[7]=0,t[8]=c,t[9]=f,t[10]=d,t[11]=0,t[12]=s,t[13]=r,t[14]=l,t[15]=1,t}function se(t,e,i){return t[0]=e[0]+i[0],t[1]=e[1]+i[1],t[2]=e[2]+i[2],t[3]=e[3]+i[3],t[4]=e[4]+i[4],t[5]=e[5]+i[5],t[6]=e[6]+i[6],t[7]=e[7]+i[7],t[8]=e[8]+i[8],t[9]=e[9]+i[9],t[10]=e[10]+i[10],t[11]=e[11]+i[11],t[12]=e[12]+i[12],t[13]=e[13]+i[13],t[14]=e[14]+i[14],t[15]=e[15]+i[15],t}function re(t,e,i){return t[0]=e[0]-i[0],t[1]=e[1]-i[1],t[2]=e[2]-i[2],t[3]=e[3]-i[3],t[4]=e[4]-i[4],t[5]=e[5]-i[5],t[6]=e[6]-i[6],t[7]=e[7]-i[7],t[8]=e[8]-i[8],t[9]=e[9]-i[9],t[10]=e[10]-i[10],t[11]=e[11]-i[11],t[12]=e[12]-i[12],t[13]=e[13]-i[13],t[14]=e[14]-i[14],t[15]=e[15]-i[15],t}function ut(t,e,i){return t[0]=e[0]*i,t[1]=e[1]*i,t[2]=e[2]*i,t[3]=e[3]*i,t[4]=e[4]*i,t[5]=e[5]*i,t[6]=e[6]*i,t[7]=e[7]*i,t[8]=e[8]*i,t[9]=e[9]*i,t[10]=e[10]*i,t[11]=e[11]*i,t[12]=e[12]*i,t[13]=e[13]*i,t[14]=e[14]*i,t[15]=e[15]*i,t}class B extends Array{constructor(e=1,i=0,n=0,s=0,r=0,l=1,a=0,h=0,o=0,c=0,f=1,d=0,g=0,p=0,u=0,m=1){return super(e,i,n,s,r,l,a,h,o,c,f,d,g,p,u,m),this}get x(){return this[12]}get y(){return this[13]}get z(){return this[14]}get w(){return this[15]}set x(e){this[12]=e}set y(e){this[13]=e}set z(e){this[14]=e}set w(e){this[15]=e}set(e,i,n,s,r,l,a,h,o,c,f,d,g,p,u,m){return e.length?this.copy(e):(tt(this,e,i,n,s,r,l,a,h,o,c,f,d,g,p,u,m),this)}translate(e,i=this){return st(this,i,e),this}rotate(e,i,n=this){return lt(this,n,e,i),this}scale(e,i=this){return rt(this,i,typeof e=="number"?[e,e,e]:e),this}add(e,i){return i?se(this,e,i):se(this,this,e),this}sub(e,i){return i?re(this,e,i):re(this,this,e),this}multiply(e,i){return e.length?i?te(this,e,i):te(this,this,e):ut(this,this,e),this}identity(){return it(this),this}copy(e){return et(this,e),this}fromPerspective({fov:e,aspect:i,near:n,far:s}={}){return dt(this,e,i,n,s),this}fromOrthogonal({left:e,right:i,bottom:n,top:s,near:r,far:l}){return gt(this,e,i,n,s,r,l),this}fromQuaternion(e){return ft(this,e),this}setPosition(e){return this.x=e[0],this.y=e[1],this.z=e[2],this}inverse(e=this){return nt(this,e),this}compose(e,i,n){return ct(this,e,i,n),this}decompose(e,i,n){return ot(this,e,i,n),this}getRotation(e){return ne(e,this),this}getTranslation(e){return at(e,this),this}getScaling(e){return ie(e,this),this}getMaxScaleOnAxis(){return ht(this)}lookAt(e,i,n){return pt(this,e,i,n),this}determinant(){return ee(this)}fromArray(e,i=0){return this[0]=e[i],this[1]=e[i+1],this[2]=e[i+2],this[3]=e[i+3],this[4]=e[i+4],this[5]=e[i+5],this[6]=e[i+6],this[7]=e[i+7],this[8]=e[i+8],this[9]=e[i+9],this[10]=e[i+10],this[11]=e[i+11],this[12]=e[i+12],this[13]=e[i+13],this[14]=e[i+14],this[15]=e[i+15],this}toArray(e=[],i=0){return e[i]=this[0],e[i+1]=this[1],e[i+2]=this[2],e[i+3]=this[3],e[i+4]=this[4],e[i+5]=this[5],e[i+6]=this[6],e[i+7]=this[7],e[i+8]=this[8],e[i+9]=this[9],e[i+10]=this[10],e[i+11]=this[11],e[i+12]=this[12],e[i+13]=this[13],e[i+14]=this[14],e[i+15]=this[15],e}}function mt(t,e,i="YXZ"){return i==="XYZ"?(t[1]=Math.asin(Math.min(Math.max(e[8],-1),1)),Math.abs(e[8])<.99999?(t[0]=Math.atan2(-e[9],e[10]),t[2]=Math.atan2(-e[4],e[0])):(t[0]=Math.atan2(e[6],e[5]),t[2]=0)):i==="YXZ"?(t[0]=Math.asin(-Math.min(Math.max(e[9],-1),1)),Math.abs(e[9])<.99999?(t[1]=Math.atan2(e[8],e[10]),t[2]=Math.atan2(e[1],e[5])):(t[1]=Math.atan2(-e[2],e[0]),t[2]=0)):i==="ZXY"?(t[0]=Math.asin(Math.min(Math.max(e[6],-1),1)),Math.abs(e[6])<.99999?(t[1]=Math.atan2(-e[2],e[10]),t[2]=Math.atan2(-e[4],e[5])):(t[1]=0,t[2]=Math.atan2(e[1],e[0]))):i==="ZYX"?(t[1]=Math.asin(-Math.min(Math.max(e[2],-1),1)),Math.abs(e[2])<.99999?(t[0]=Math.atan2(e[6],e[10]),t[2]=Math.atan2(e[1],e[0])):(t[0]=0,t[2]=Math.atan2(-e[4],e[5]))):i==="YZX"?(t[2]=Math.asin(Math.min(Math.max(e[1],-1),1)),Math.abs(e[1])<.99999?(t[0]=Math.atan2(-e[9],e[5]),t[1]=Math.atan2(-e[2],e[0])):(t[0]=0,t[1]=Math.atan2(e[8],e[10]))):i==="XZY"&&(t[2]=Math.asin(-Math.min(Math.max(e[4],-1),1)),Math.abs(e[4])<.99999?(t[0]=Math.atan2(e[6],e[5]),t[1]=Math.atan2(e[8],e[0])):(t[0]=Math.atan2(-e[9],e[10]),t[1]=0)),t}const le=new B;class vt extends Array{constructor(e=0,i=e,n=e,s="YXZ"){super(e,i,n),this.order=s,this.onChange=()=>{},this._target=this;const r=["0","1","2"];return new Proxy(this,{set(l,a){const h=Reflect.set(...arguments);return h&&r.includes(a)&&l.onChange(),h}})}get x(){return this[0]}get y(){return this[1]}get z(){return this[2]}set x(e){this._target[0]=e,this.onChange()}set y(e){this._target[1]=e,this.onChange()}set z(e){this._target[2]=e,this.onChange()}set(e,i=e,n=e){return e.length?this.copy(e):(this._target[0]=e,this._target[1]=i,this._target[2]=n,this.onChange(),this)}copy(e){return this._target[0]=e[0],this._target[1]=e[1],this._target[2]=e[2],this.onChange(),this}reorder(e){return this._target.order=e,this.onChange(),this}fromRotationMatrix(e,i=this.order){return mt(this._target,e,i),this.onChange(),this}fromQuaternion(e,i=this.order,n){return le.fromQuaternion(e),this._target.fromRotationMatrix(le,i),n||this.onChange(),this}fromArray(e,i=0){return this._target[0]=e[i],this._target[1]=e[i+1],this._target[2]=e[i+2],this}toArray(e=[],i=0){return e[i]=this[0],e[i+1]=this[1],e[i+2]=this[2],e}}class xt{constructor(){this.parent=null,this.children=[],this.visible=!0,this.matrix=new B,this.worldMatrix=new B,this.matrixAutoUpdate=!0,this.worldMatrixNeedsUpdate=!1,this.position=new T,this.quaternion=new Ke,this.scale=new T(1),this.rotation=new vt,this.up=new T(0,1,0),this.rotation._target.onChange=()=>this.quaternion.fromEuler(this.rotation,!0),this.quaternion._target.onChange=()=>this.rotation.fromQuaternion(this.quaternion,void 0,!0)}setParent(e,i=!0){this.parent&&e!==this.parent&&this.parent.removeChild(this,!1),this.parent=e,i&&e&&e.addChild(this,!1)}addChild(e,i=!0){~this.children.indexOf(e)||this.children.push(e),i&&e.setParent(this,!1)}removeChild(e,i=!0){~this.children.indexOf(e)&&this.children.splice(this.children.indexOf(e),1),i&&e.setParent(null,!1)}updateMatrixWorld(e){this.matrixAutoUpdate&&this.updateMatrix(),(this.worldMatrixNeedsUpdate||e)&&(this.parent===null?this.worldMatrix.copy(this.matrix):this.worldMatrix.multiply(this.parent.worldMatrix,this.matrix),this.worldMatrixNeedsUpdate=!1,e=!0);for(let i=0,n=this.children.length;i<n;i++)this.children[i].updateMatrixWorld(e)}updateMatrix(){this.matrix.compose(this.quaternion,this.position,this.scale),this.worldMatrixNeedsUpdate=!0}traverse(e){if(!e(this))for(let i=0,n=this.children.length;i<n;i++)this.children[i].traverse(e)}decompose(){this.matrix.decompose(this.quaternion._target,this.position,this.scale),this.rotation.fromQuaternion(this.quaternion)}lookAt(e,i=!1){i?this.matrix.lookAt(this.position,e,this.up):this.matrix.lookAt(e,this.position,this.up),this.matrix.getRotation(this.quaternion._target),this.rotation.fromQuaternion(this.quaternion)}}function yt(t,e){return t[0]=e[0],t[1]=e[1],t[2]=e[2],t[3]=e[4],t[4]=e[5],t[5]=e[6],t[6]=e[8],t[7]=e[9],t[8]=e[10],t}function wt(t,e){let i=e[0],n=e[1],s=e[2],r=e[3],l=i+i,a=n+n,h=s+s,o=i*l,c=n*l,f=n*a,d=s*l,g=s*a,p=s*h,u=r*l,m=r*a,v=r*h;return t[0]=1-f-p,t[3]=c-v,t[6]=d+m,t[1]=c+v,t[4]=1-o-p,t[7]=g-u,t[2]=d-m,t[5]=g+u,t[8]=1-o-f,t}function bt(t,e){return t[0]=e[0],t[1]=e[1],t[2]=e[2],t[3]=e[3],t[4]=e[4],t[5]=e[5],t[6]=e[6],t[7]=e[7],t[8]=e[8],t}function Ct(t,e,i,n,s,r,l,a,h,o){return t[0]=e,t[1]=i,t[2]=n,t[3]=s,t[4]=r,t[5]=l,t[6]=a,t[7]=h,t[8]=o,t}function Mt(t){return t[0]=1,t[1]=0,t[2]=0,t[3]=0,t[4]=1,t[5]=0,t[6]=0,t[7]=0,t[8]=1,t}function St(t,e){let i=e[0],n=e[1],s=e[2],r=e[3],l=e[4],a=e[5],h=e[6],o=e[7],c=e[8],f=c*l-a*o,d=-c*r+a*h,g=o*r-l*h,p=i*f+n*d+s*g;return p?(p=1/p,t[0]=f*p,t[1]=(-c*n+s*o)*p,t[2]=(a*n-s*l)*p,t[3]=d*p,t[4]=(c*i-s*h)*p,t[5]=(-a*i+s*r)*p,t[6]=g*p,t[7]=(-o*i+n*h)*p,t[8]=(l*i-n*r)*p,t):null}function ae(t,e,i){let n=e[0],s=e[1],r=e[2],l=e[3],a=e[4],h=e[5],o=e[6],c=e[7],f=e[8],d=i[0],g=i[1],p=i[2],u=i[3],m=i[4],v=i[5],M=i[6],y=i[7],x=i[8];return t[0]=d*n+g*l+p*o,t[1]=d*s+g*a+p*c,t[2]=d*r+g*h+p*f,t[3]=u*n+m*l+v*o,t[4]=u*s+m*a+v*c,t[5]=u*r+m*h+v*f,t[6]=M*n+y*l+x*o,t[7]=M*s+y*a+x*c,t[8]=M*r+y*h+x*f,t}function At(t,e,i){let n=e[0],s=e[1],r=e[2],l=e[3],a=e[4],h=e[5],o=e[6],c=e[7],f=e[8],d=i[0],g=i[1];return t[0]=n,t[1]=s,t[2]=r,t[3]=l,t[4]=a,t[5]=h,t[6]=d*n+g*l+o,t[7]=d*s+g*a+c,t[8]=d*r+g*h+f,t}function Et(t,e,i){let n=e[0],s=e[1],r=e[2],l=e[3],a=e[4],h=e[5],o=e[6],c=e[7],f=e[8],d=Math.sin(i),g=Math.cos(i);return t[0]=g*n+d*l,t[1]=g*s+d*a,t[2]=g*r+d*h,t[3]=g*l-d*n,t[4]=g*a-d*s,t[5]=g*h-d*r,t[6]=o,t[7]=c,t[8]=f,t}function _t(t,e,i){let n=i[0],s=i[1];return t[0]=n*e[0],t[1]=n*e[1],t[2]=n*e[2],t[3]=s*e[3],t[4]=s*e[4],t[5]=s*e[5],t[6]=e[6],t[7]=e[7],t[8]=e[8],t}function zt(t,e){let i=e[0],n=e[1],s=e[2],r=e[3],l=e[4],a=e[5],h=e[6],o=e[7],c=e[8],f=e[9],d=e[10],g=e[11],p=e[12],u=e[13],m=e[14],v=e[15],M=i*a-n*l,y=i*h-s*l,x=i*o-r*l,b=n*h-s*a,w=n*o-r*a,S=s*o-r*h,_=c*u-f*p,z=c*m-d*p,A=c*v-g*p,F=f*m-d*u,E=f*v-g*u,D=d*v-g*m,C=M*D-y*E+x*F+b*A-w*z+S*_;return C?(C=1/C,t[0]=(a*D-h*E+o*F)*C,t[1]=(h*A-l*D-o*z)*C,t[2]=(l*E-a*A+o*_)*C,t[3]=(s*E-n*D-r*F)*C,t[4]=(i*D-s*A+r*z)*C,t[5]=(n*A-i*E-r*_)*C,t[6]=(u*S-m*w+v*b)*C,t[7]=(m*x-p*S-v*y)*C,t[8]=(p*w-u*x+v*M)*C,t):null}class Ft extends Array{constructor(e=1,i=0,n=0,s=0,r=1,l=0,a=0,h=0,o=1){return super(e,i,n,s,r,l,a,h,o),this}set(e,i,n,s,r,l,a,h,o){return e.length?this.copy(e):(Ct(this,e,i,n,s,r,l,a,h,o),this)}translate(e,i=this){return At(this,i,e),this}rotate(e,i=this){return Et(this,i,e),this}scale(e,i=this){return _t(this,i,e),this}multiply(e,i){return i?ae(this,e,i):ae(this,this,e),this}identity(){return Mt(this),this}copy(e){return bt(this,e),this}fromMatrix4(e){return yt(this,e),this}fromQuaternion(e){return wt(this,e),this}fromBasis(e,i,n){return this.set(e[0],e[1],e[2],i[0],i[1],i[2],n[0],n[1],n[2]),this}inverse(e=this){return St(this,e),this}getNormalMatrix(e){return zt(this,e),this}}let Dt=0;class Tt extends xt{constructor(e,{geometry:i,program:n,mode:s=e.TRIANGLES,frustumCulled:r=!0,renderOrder:l=0}={}){super(),e.canvas||console.error("gl not passed as first argument to Mesh"),this.gl=e,this.id=Dt++,this.geometry=i,this.program=n,this.mode=s,this.frustumCulled=r,this.renderOrder=l,this.modelViewMatrix=new B,this.normalMatrix=new Ft,this.beforeRenderCallbacks=[],this.afterRenderCallbacks=[]}onBeforeRender(e){return this.beforeRenderCallbacks.push(e),this}onAfterRender(e){return this.afterRenderCallbacks.push(e),this}draw({camera:e}={}){e&&(this.program.uniforms.modelMatrix||Object.assign(this.program.uniforms,{modelMatrix:{value:null},viewMatrix:{value:null},modelViewMatrix:{value:null},normalMatrix:{value:null},projectionMatrix:{value:null},cameraPosition:{value:null}}),this.program.uniforms.projectionMatrix.value=e.projectionMatrix,this.program.uniforms.cameraPosition.value=e.worldPosition,this.program.uniforms.viewMatrix.value=e.viewMatrix,this.modelViewMatrix.multiply(e.viewMatrix,this.worldMatrix),this.normalMatrix.getNormalMatrix(this.modelViewMatrix),this.program.uniforms.modelMatrix.value=this.worldMatrix,this.program.uniforms.modelViewMatrix.value=this.modelViewMatrix,this.program.uniforms.normalMatrix.value=this.normalMatrix),this.beforeRenderCallbacks.forEach(n=>n&&n({mesh:this,camera:e}));let i=this.program.cullFace&&this.worldMatrix.determinant()<0;this.program.use({flipFaces:i}),this.geometry.draw({mode:this.mode,program:this.program}),this.afterRenderCallbacks.forEach(n=>n&&n({mesh:this,camera:e}))}}class Rt extends Ee{constructor(e,{attributes:i={}}={}){Object.assign(i,{position:{size:2,data:new Float32Array([-1,-1,3,-1,-1,3])},uv:{size:2,data:new Float32Array([0,0,2,0,0,2])}}),super(e,i)}}const kt=`attribute vec2 position;
attribute vec2 uv;
varying vec2 vUv;

void main() {
    vUv = uv;
    gl_Position = vec4(position, 0.0, 1.0);
}
`,Ot=`precision highp float;

varying vec2 vUv;

uniform float uTime;
uniform vec3 uColor1;
uniform vec3 uColor2;
uniform vec3 uColor3;
uniform vec3 uColor4;
uniform vec3 uColor5;
uniform float uNoiseStrength;
uniform float uMode; // 0=Mesh, 1=Aurora, 2=Grainy, 3=DeepSea, 4=Holographic, 5=Flow, 6=Radiant, 7=Kaleidoscope

// Simplex 2D noise
vec3 permute(vec3 x) { return mod(((x*34.0)+1.0)*x, 289.0); }

float snoise(vec2 v){
  const vec4 C = vec4(0.211324865405187, 0.366025403784439,
           -0.577350269189626, 0.024390243902439);
  vec2 i  = floor(v + dot(v, C.yy) );
  vec2 x0 = v - i + dot(i, C.xx);
  vec2 i1;
  i1 = (x0.x > x0.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
  vec4 x12 = x0.xyxy + C.xxzz;
  x12.xy -= i1;
  i = mod(i, 289.0);
  vec3 p = permute( permute( i.y + vec3(0.0, i1.y, 1.0 ))
  + i.x + vec3(0.0, i1.x, 1.0 ));
  vec3 m = max(0.5 - vec3(dot(x0,x0), dot(x12.xy,x12.xy), dot(x12.zw,x12.zw)), 0.0);
  m = m*m ;
  m = m*m ;
  vec3 x = 2.0 * fract(p * C.www) - 1.0;
  vec3 h = abs(x) - 0.5;
  vec3 ox = floor(x + 0.5);
  vec3 a0 = x - ox;
  m *= 1.79284291400159 - 0.85373472095314 * ( a0*a0 + h*h );
  vec3 g;
  g.x  = a0.x  * x0.x  + h.x  * x0.y;
  g.yz = a0.yz * x12.xz + h.yz * x12.yw;
  return 130.0 * dot(m, g);
}

// Hue Shift
vec3 hueShift(vec3 color, float shift) {
    vec3 k = vec3(0.57735, 0.57735, 0.57735);
    float cosAngle = cos(shift);
    return vec3(color * cosAngle + cross(k, color) * sin(shift) + k * dot(k, color) * (1.0 - cosAngle));
}

// Voronoi / Caustics
vec2 hash2(vec2 p) {
    return fract(sin(vec2(dot(p,vec2(127.1,311.7)),dot(p,vec2(269.5,183.3))))*43758.5453);
}

float voronoi(vec2 uv, float time) {
    vec2 n = floor(uv);
    vec2 f = fract(uv);
    float m = 1.0;
    for(int j=-1; j<=1; j++) {
        for(int i=-1; i<=1; i++) {
            vec2 g = vec2(float(i), float(j));
            vec2 o = hash2(n + g);
            o = 0.5 + 0.5 * sin(time + 6.2831 * o);
            vec2 r = g - f + o;
            float d = length(r);
            m = min(m, d);
        }
    }
    return m;
}

// Vibrance boost — selectively saturates desaturated midtones to prevent muddy blending
vec3 vibranceBoost(vec3 c, float amount) {
    float luma = dot(c, vec3(0.2126, 0.7152, 0.0722));
    float maxC = max(c.r, max(c.g, c.b));
    float minC = min(c.r, min(c.g, c.b));
    float sat = (maxC > 0.001) ? (maxC - minC) / maxC : 0.0;
    float boost = amount * (1.0 - sat);
    return mix(vec3(luma), c, 1.0 + boost);
}

void main() {
    vec2 uv = vUv;
    float time = uTime * 0.2;
    vec3 color = vec3(0.0);

    if (uMode < 0.5) {
         // --- MESH MODE (0) ---
         float noiseScale = 1.5;
         float warpStrength = 0.2;
         float moveScale = 0.1;
         float falloff = 0.1;

         float n = snoise(vec2(uv.x * noiseScale + time, uv.y * noiseScale - time));
         vec2 warpedUv = uv + vec2(n * warpStrength);

         vec2 p0 = vec2(0.5, 0.5);
         vec2 p1 = vec2(0.2, 0.2) + vec2(sin(time * 0.5) * moveScale, cos(time * 0.6) * moveScale);
         vec2 p2 = vec2(0.8, 0.2) + vec2(cos(time * 0.7) * moveScale, sin(time * 0.4) * moveScale);
         vec2 p3 = vec2(0.2, 0.8) + vec2(sin(time * 0.3) * moveScale, cos(time * 0.5) * moveScale);
         vec2 p4 = vec2(0.8, 0.8) + vec2(cos(time * 0.4) * moveScale, sin(time * 0.6) * moveScale);

         float w0 = 1.0 / (length(warpedUv - p0) * length(warpedUv - p0) + falloff);
         float w1 = 1.0 / (length(warpedUv - p1) * length(warpedUv - p1) + falloff);
         float w2 = 1.0 / (length(warpedUv - p2) * length(warpedUv - p2) + falloff);
         float w3 = 1.0 / (length(warpedUv - p3) * length(warpedUv - p3) + falloff);
         float w4 = 1.0 / (length(warpedUv - p4) * length(warpedUv - p4) + falloff);

         float total = w0 + w1 + w2 + w3 + w4;
         color = (uColor1 * w0 + uColor2 * w1 + uColor3 * w2 + uColor4 * w3 + uColor5 * w4) / total;

    } else if (uMode > 0.5 && uMode < 1.5) {
         // --- AURORA MODE (1) - Dreamy Northern Lights ---
         // FBM for organic curtain displacement (not rigid sine waves)
         float fbm = snoise(vec2(uv.x * 1.2 + time * 0.1, uv.y * 0.5)) * 0.5
                   + snoise(vec2(uv.x * 2.5 - time * 0.15, uv.y * 0.8 + time * 0.05)) * 0.25
                   + snoise(vec2(uv.x * 5.0 + time * 0.08, uv.y * 1.5)) * 0.125;

         // Displace vertical position with noise for organic band shapes
         float dy = uv.y + fbm * 0.15;

         // Wide, heavily overlapping bands — colors bleed into each other
         float band1 = smoothstep(0.0, 0.35, dy) * smoothstep(0.65, 0.25, dy);
         float band2 = smoothstep(0.15, 0.50, dy) * smoothstep(0.80, 0.40, dy);
         float band3 = smoothstep(0.30, 0.60, dy) * smoothstep(0.90, 0.55, dy);
         float band4 = smoothstep(0.45, 0.70, dy) * smoothstep(1.0, 0.65, dy);

         // Soft shimmer — low frequency noise modulation
         float shimmer = snoise(vec2(uv.x * 3.0 + time * 0.4, uv.y * 1.5 - time * 0.1)) * 0.5 + 0.5;
         shimmer = shimmer * 0.6 + 0.4;

         // Base: weighted mesh blend so user colors are always visible
         float moveScale = 0.2;
         float falloff = 0.3;
         vec2 wUv = uv + vec2(fbm * 0.1);
         vec2 p0 = vec2(0.5, 0.5);
         vec2 p1 = vec2(0.2, 0.3) + vec2(sin(time * 0.3) * moveScale, cos(time * 0.4) * moveScale);
         vec2 p2 = vec2(0.8, 0.3) + vec2(cos(time * 0.5) * moveScale, sin(time * 0.3) * moveScale);
         vec2 p3 = vec2(0.2, 0.7) + vec2(sin(time * 0.2) * moveScale, cos(time * 0.3) * moveScale);
         vec2 p4 = vec2(0.8, 0.7) + vec2(cos(time * 0.3) * moveScale, sin(time * 0.4) * moveScale);
         float bw0 = 1.0 / (length(wUv - p0) * length(wUv - p0) + falloff);
         float bw1 = 1.0 / (length(wUv - p1) * length(wUv - p1) + falloff);
         float bw2 = 1.0 / (length(wUv - p2) * length(wUv - p2) + falloff);
         float bw3 = 1.0 / (length(wUv - p3) * length(wUv - p3) + falloff);
         float bw4 = 1.0 / (length(wUv - p4) * length(wUv - p4) + falloff);
         float bTotal = bw0 + bw1 + bw2 + bw3 + bw4;
         vec3 baseGrad = (uColor1*bw0 + uColor2*bw1 + uColor3*bw2 + uColor4*bw3 + uColor5*bw4) / bTotal;

         // Aurora glow: each band carries a different user color
         vec3 auroraGlow = vec3(0.0);
         auroraGlow += uColor2 * band1 * shimmer * 0.6;
         auroraGlow += uColor3 * band2 * shimmer * 0.5;
         auroraGlow += uColor4 * band3 * shimmer * 0.45;
         auroraGlow += uColor5 * band4 * shimmer * 0.4;

         // Vertical fade so edges are soft
         float vFade = smoothstep(0.0, 0.25, uv.y) * smoothstep(1.0, 0.75, uv.y);

         // Composite: base gradient + additive aurora glow + color interaction
         color = baseGrad * 0.6 + auroraGlow * vFade + baseGrad * auroraGlow * 0.5;

    } else if (uMode > 1.5 && uMode < 2.5) {
         // --- GRAINY MODE (2) - Gradient-First with Animated Artistic Grain ---
         // Step 1: Smooth mesh gradient (base, identical to mode 0)
         float noiseScale = 1.5;
         float warpStrength = 0.2;
         float moveScale = 0.1;
         float falloff = 0.1;

         float n = snoise(vec2(uv.x * noiseScale + time, uv.y * noiseScale - time));
         vec2 warpedUv = uv + vec2(n * warpStrength);

         vec2 p0 = vec2(0.5, 0.5);
         vec2 p1 = vec2(0.2, 0.2) + vec2(sin(time * 0.5) * moveScale, cos(time * 0.6) * moveScale);
         vec2 p2 = vec2(0.8, 0.2) + vec2(cos(time * 0.7) * moveScale, sin(time * 0.4) * moveScale);
         vec2 p3 = vec2(0.2, 0.8) + vec2(sin(time * 0.3) * moveScale, cos(time * 0.5) * moveScale);
         vec2 p4 = vec2(0.8, 0.8) + vec2(cos(time * 0.4) * moveScale, sin(time * 0.6) * moveScale);

         float w0 = 1.0 / (length(warpedUv - p0) * length(warpedUv - p0) + falloff);
         float w1 = 1.0 / (length(warpedUv - p1) * length(warpedUv - p1) + falloff);
         float w2 = 1.0 / (length(warpedUv - p2) * length(warpedUv - p2) + falloff);
         float w3 = 1.0 / (length(warpedUv - p3) * length(warpedUv - p3) + falloff);
         float w4 = 1.0 / (length(warpedUv - p4) * length(warpedUv - p4) + falloff);

         float total = w0 + w1 + w2 + w3 + w4;
         vec3 smoothGrad = (uColor1 * w0 + uColor2 * w1 + uColor3 * w2 + uColor4 * w3 + uColor5 * w4) / total;

         // Step 2: Animated stipple overlay (risograph/printed paper texture)
         // Animate grain by slowly drifting the cell coordinate space
         float grainTime = time * 0.3;
         vec2 grainDrift = vec2(
             sin(grainTime * 0.7) * 30.0 + grainTime * 8.0,
             cos(grainTime * 0.5) * 25.0 + grainTime * 5.0
         );
         vec2 animatedCoord = gl_FragCoord.xy + grainDrift;

         // Multi-scale grain: large and small particles
         float cellSizeLg = 200.0;
         float cellSizeSm = 80.0;

         // Large grain layer
         vec2 cellCoord = floor(animatedCoord / cellSizeLg);
         vec2 cellFrac = fract(animatedCoord / cellSizeLg);

         float cellRand1 = fract(sin(dot(cellCoord, vec2(127.1, 311.7))) * 43758.5453);
         float cellRand2 = fract(sin(dot(cellCoord, vec2(269.5, 183.3))) * 43758.5453);
         float cellRand3 = fract(sin(dot(cellCoord, vec2(419.2, 371.9))) * 43758.5453);

         vec3 chosenColor;
         if (cellRand1 < 0.2) chosenColor = uColor1;
         else if (cellRand1 < 0.4) chosenColor = uColor2;
         else if (cellRand1 < 0.6) chosenColor = uColor3;
         else if (cellRand1 < 0.8) chosenColor = uColor4;
         else chosenColor = uColor5;

         vec3 stippleColor = mix(chosenColor, smoothGrad, 0.5);

         vec2 dotCenter = vec2(cellRand2, cellRand3);
         float dotRadius = 0.2 + cellRand2 * 0.15;
         float dist = length(cellFrac - dotCenter);
         float dotMask = smoothstep(dotRadius, dotRadius - 0.05, dist);

         // Small grain layer (finer stipple, faster drift)
         vec2 grainDrift2 = vec2(
             cos(grainTime * 0.9) * 15.0 - grainTime * 6.0,
             sin(grainTime * 0.6) * 20.0 + grainTime * 4.0
         );
         vec2 animCoord2 = gl_FragCoord.xy + grainDrift2;
         vec2 cellCoord2 = floor(animCoord2 / cellSizeSm);
         vec2 cellFrac2 = fract(animCoord2 / cellSizeSm);

         float cRand2a = fract(sin(dot(cellCoord2, vec2(217.3, 131.5))) * 43758.5453);
         float cRand2b = fract(sin(dot(cellCoord2, vec2(169.7, 283.1))) * 43758.5453);
         float cRand2c = fract(sin(dot(cellCoord2, vec2(319.4, 471.2))) * 43758.5453);

         vec3 chosenColor2;
         if (cRand2a < 0.25) chosenColor2 = uColor1;
         else if (cRand2a < 0.5) chosenColor2 = uColor2;
         else if (cRand2a < 0.75) chosenColor2 = uColor3;
         else chosenColor2 = uColor4;

         vec3 stippleColor2 = mix(chosenColor2, smoothGrad, 0.6);
         vec2 dotCenter2 = vec2(cRand2b, cRand2c);
         float dotRadius2 = 0.15 + cRand2b * 0.1;
         float dist2 = length(cellFrac2 - dotCenter2);
         float dotMask2 = smoothstep(dotRadius2, dotRadius2 - 0.04, dist2);

         // Composite: gradient base + large stipple + fine stipple
         color = smoothGrad;
         color = mix(color, stippleColor, dotMask * 0.3);
         color = mix(color, stippleColor2, dotMask2 * 0.18);

    } else if (uMode > 2.5 && uMode < 3.5) {
        // --- DEEP SEA MODE (Voronoi Caustics) ---
        // Layer 1: Slow swell (Simplex)
        float swell = snoise(vec2(uv.x * 1.5 + time * 0.3, uv.y * 1.0 - time * 0.2));

        // Base Gradient: Mix 1, 2, and 5 (Depth gradient)
        vec3 baseColor = mix(uColor1, uColor2, swell * 0.5 + 0.5);
        baseColor = mix(baseColor, uColor5, uv.y * 0.6); // Add 5th color to bottom/depth

        // Layer 2: Caustics (Inverted Voronoi)
        float v = voronoi(uv * 4.0, time * 0.8);
        float caustic = pow(1.0 - v, 4.0);

        // Caustic Color: Mix 3 (Primary)
        color = baseColor + uColor3 * caustic * 0.8;

        // Layer 3: Secondary Caustics (Depth)
        float v2 = voronoi(uv * 6.0 + vec2(time), time * 1.2);
        // Mix 4 for secondary lights
        color += uColor4 * pow(1.0 - v2, 3.0) * 0.4;

        // Vignette
        float dist = distance(uv, vec2(0.5));
        color *= (1.0 - dist * 0.6);

    } else if (uMode > 3.5 && uMode < 4.5) {
        // --- HOLOGRAPHIC MODE - Soap Bubble / Iridescent Film ---
        // Step 1: Mesh gradient base (user colors always dominant)
        float hMoveScale = 0.15;
        float hFalloff = 0.15;
        vec2 hp0 = vec2(0.5, 0.5);
        vec2 hp1 = vec2(0.2, 0.25) + vec2(sin(time * 0.4) * hMoveScale, cos(time * 0.5) * hMoveScale);
        vec2 hp2 = vec2(0.8, 0.25) + vec2(cos(time * 0.6) * hMoveScale, sin(time * 0.35) * hMoveScale);
        vec2 hp3 = vec2(0.2, 0.75) + vec2(sin(time * 0.25) * hMoveScale, cos(time * 0.45) * hMoveScale);
        vec2 hp4 = vec2(0.8, 0.75) + vec2(cos(time * 0.35) * hMoveScale, sin(time * 0.55) * hMoveScale);
        float hw0 = 1.0 / (length(uv - hp0) * length(uv - hp0) + hFalloff);
        float hw1 = 1.0 / (length(uv - hp1) * length(uv - hp1) + hFalloff);
        float hw2 = 1.0 / (length(uv - hp2) * length(uv - hp2) + hFalloff);
        float hw3 = 1.0 / (length(uv - hp3) * length(uv - hp3) + hFalloff);
        float hw4 = 1.0 / (length(uv - hp4) * length(uv - hp4) + hFalloff);
        float hTotal = hw0 + hw1 + hw2 + hw3 + hw4;
        vec3 holoBase = (uColor1*hw0 + uColor2*hw1 + uColor3*hw2 + uColor4*hw3 + uColor5*hw4) / hTotal;

        // Step 2: Soap bubble thin-film interference
        // Multiple interference layers at different scales for organic filament look
        float filmNoise1 = snoise(uv * 3.0 + vec2(time * 0.06, time * 0.04));
        float filmNoise2 = snoise(uv * 5.5 - vec2(time * 0.08, -time * 0.05));
        float filmNoise3 = snoise(uv * 8.0 + vec2(-time * 0.04, time * 0.07));

        // Optical path difference — multiple layers create swirling filaments like soap film
        float opd = (uv.x * 4.0 + uv.y * 3.0) * 1.8
                   + filmNoise1 * 2.2
                   + filmNoise2 * 1.1
                   + filmNoise3 * 0.5;

        // Thin-film spectral colors — cycle through the full rainbow
        // These are physically-inspired interference fringes
        vec3 thinFilm;
        thinFilm.r = sin(opd * 3.14159 * 2.0) * 0.5 + 0.5;
        thinFilm.g = sin(opd * 3.14159 * 2.0 + 2.094) * 0.5 + 0.5;
        thinFilm.b = sin(opd * 3.14159 * 2.0 + 4.189) * 0.5 + 0.5;

        // Also hue-shift the base gradient for harmony
        vec3 filmShifted = hueShift(holoBase, opd * 1.8 + time * 0.25);

        // Blend the raw thin-film spectrum with the palette-shifted version
        vec3 filmColor = mix(filmShifted, thinFilm, 0.35);

        // Step 3: Spatial variation — organic patches where interference is strong
        float patchNoise = snoise(uv * 2.5 + vec2(time * 0.1, time * 0.08)) * 0.5 + 0.5;
        float patchNoise2 = snoise(uv * 4.0 - vec2(time * 0.15)) * 0.5 + 0.5;
        float intensity = smoothstep(0.2, 0.8, patchNoise) * (0.6 + patchNoise2 * 0.4);

        // Step 4: Fresnel effect — stronger at edges like a real soap bubble
        float viewAngle = abs(dot(normalize(vec3(uv - 0.5, 0.8)), vec3(0.0, 0.0, 1.0)));
        float fresnel = pow(1.0 - viewAngle, 1.8);

        // Step 5: Flowing filament streaks (soap bubble drainage lines)
        float streak1 = snoise(vec2(uv.x * 1.5 + time * 0.15, uv.y * 10.0 + time * 0.08)) * 0.5 + 0.5;
        float streak2 = snoise(vec2(uv.x * 2.0 - time * 0.1, uv.y * 6.0 - time * 0.12)) * 0.5 + 0.5;
        float streakMask = pow(streak1, 2.5) * 0.18 + pow(streak2, 3.0) * 0.10;

        // Step 6: Specular highlight (moving gloss spot)
        float gloss = snoise(vec2(uv.x * 3.0 - time * 0.3, uv.y * 3.0 + time * 0.2));
        gloss = pow(max(0.0, gloss), 4.0) * 0.15;

        // Composite: stronger film interference blended with base
        float filmStrength = intensity * (fresnel * 0.5 + 0.5) * 0.55;
        color = mix(holoBase, filmColor, filmStrength);
        color += hueShift(holoBase, streakMask * 6.0) * streakMask; // iridescent streaks
        color += vec3(gloss); // specular highlight

    } else if (uMode > 4.5 && uMode < 5.5) {
        // --- IMPASTO MODE (Oil Painting on Canvas) ---
        // 1. Brush stroke height map — directional strokes at multiple scales
        // Slow time for oil painting feel (paint doesn't move fast)
        float oilTime = time * 0.3;

        // Directional brush strokes (anisotropic noise — stretched along stroke direction)
        float strokeAngle = 0.4 + snoise(uv * 0.8 + vec2(oilTime * 0.02)) * 0.3;
        vec2 strokeDir = vec2(cos(strokeAngle), sin(strokeAngle));
        vec2 strokePerp = vec2(-strokeDir.y, strokeDir.x);

        // Stroke-aligned UV for anisotropic noise
        vec2 strokeUv = vec2(dot(uv, strokeDir), dot(uv, strokePerp));

        // Large palette knife strokes
        float n1 = snoise(vec2(strokeUv.x * 2.0 + oilTime * 0.04, strokeUv.y * 4.0));
        // Medium brush strokes
        float n2 = snoise(vec2(strokeUv.x * 5.0 - oilTime * 0.06, strokeUv.y * 8.0 + oilTime * 0.03));
        // Fine bristle detail
        float n3 = snoise(vec2(strokeUv.x * 12.0 + oilTime * 0.05, strokeUv.y * 18.0));
        // Very fine canvas weave texture
        float canvasWeave = snoise(uv * 40.0) * 0.3 + snoise(uv * 60.0 + vec2(0.5)) * 0.15;

        // Combine: thick impasto strokes dominate, canvas weave is subtle
        float height = n1 * 0.45 + n2 * 0.3 + n3 * 0.15 + canvasWeave * 0.10;

        // 2. Surface normals via finite difference (thicker epsilon for paint ridges)
        vec2 d = vec2(0.004, 0.0);

        // Height at offset positions
        vec2 uvDx = uv + d;
        vec2 uvDy = uv + d.yx;
        vec2 sDx = vec2(dot(uvDx, strokeDir), dot(uvDx, strokePerp));
        vec2 sDy = vec2(dot(uvDy, strokeDir), dot(uvDy, strokePerp));

        float hxVal = snoise(vec2(sDx.x * 2.0 + oilTime * 0.04, sDx.y * 4.0)) * 0.45
                    + snoise(vec2(sDx.x * 5.0 - oilTime * 0.06, sDx.y * 8.0 + oilTime * 0.03)) * 0.3
                    + snoise(vec2(sDx.x * 12.0 + oilTime * 0.05, sDx.y * 18.0)) * 0.15
                    + (snoise(uvDx * 40.0) * 0.3 + snoise(uvDx * 60.0 + vec2(0.5)) * 0.15) * 0.10;
        float hyVal = snoise(vec2(sDy.x * 2.0 + oilTime * 0.04, sDy.y * 4.0)) * 0.45
                    + snoise(vec2(sDy.x * 5.0 - oilTime * 0.06, sDy.y * 8.0 + oilTime * 0.03)) * 0.3
                    + snoise(vec2(sDy.x * 12.0 + oilTime * 0.05, sDy.y * 18.0)) * 0.15
                    + (snoise(uvDy * 40.0) * 0.3 + snoise(uvDy * 60.0 + vec2(0.5)) * 0.15) * 0.10;

        float hx = hxVal - height;
        float hy = hyVal - height;
        vec3 normal = normalize(vec3(hx * 7.0, hy * 7.0, 1.0)); // Stronger normals for thick paint

        // 3. Dual-light setup (warm key + cool fill, like a gallery)
        vec3 keyLightDir = normalize(vec3(-0.4, 0.6, 1.0));
        vec3 fillLightDir = normalize(vec3(0.5, -0.3, 0.8));
        float keyDiffuse = max(dot(normal, keyLightDir), 0.0);
        float fillDiffuse = max(dot(normal, fillLightDir), 0.0);
        float diffuse = keyDiffuse * 0.7 + fillDiffuse * 0.3;

        // Specular: glossy wet oil paint reflection
        vec3 viewDir = vec3(0.0, 0.0, 1.0);
        float keySpec = pow(max(dot(reflect(-keyLightDir, normal), viewDir), 0.0), 16.0);
        float fillSpec = pow(max(dot(reflect(-fillLightDir, normal), viewDir), 0.0), 12.0);
        float specular = keySpec * 0.25 + fillSpec * 0.08;

        // 4. Color mixing — thick paint smearing along stroke direction
        vec2 smearUv = uv + normal.xy * 0.12 + strokeDir * n1 * 0.04;

        // Paint regions defined by noise (like palette knife application areas)
        float region1 = snoise(smearUv * 2.0 + vec2(oilTime * 0.02));
        float region2 = snoise(smearUv * 3.5 - vec2(oilTime * 0.03));

        vec3 cBase = mix(uColor1, uColor2, smoothstep(-0.3, 0.7, smearUv.y + region1 * 0.2));
        cBase = mix(cBase, uColor3, smoothstep(0.1, 0.7, region1));
        cBase = mix(cBase, uColor4, smoothstep(0.3, 0.7, region2) * 0.7);

        // Paint thickness affects saturation (thicker = richer, impasto effect)
        float thickness = smoothstep(-0.3, 0.5, height);
        cBase = mix(cBase * 0.9, cBase * 1.1, thickness);

        // 5. Composition: paint + lighting + canvas
        color = cBase * (0.65 + 0.35 * diffuse);
        color += vec3(specular) * vec3(1.0, 0.97, 0.92); // Warm-tinted specular
        color = mix(color, uColor5, smoothstep(0.65, 1.0, height) * 0.4); // Highlight peaks

        // Subtle darkening in crevices between strokes
        float ao = smoothstep(-0.5, 0.1, height);
        color *= 0.85 + 0.15 * ao;

    } else if (uMode > 5.5 && uMode < 6.5) {
        // --- SPECTRAL MODE (Prismatic Glass) ---
        // 1. Create a "glass" distortion field
        float glassNoise = snoise(uv * 1.5 + vec2(time * 0.1));
        vec2 glassDistort = vec2(glassNoise) * 0.1;

        // 2. Strong Dispersion (Chromatic Aberration) based on distortion
        // The more distorted the area, the more color separation we want
        float dispersionStrength = 0.03 + 0.05 * abs(glassNoise);

        // 3. Channel Splitting with Domain Warping
        // We distort each channel's UV lookup differently
        vec2 uvR = uv + glassDistort * (1.0 + dispersionStrength);
        vec2 uvG = uv + glassDistort;
        vec2 uvB = uv + glassDistort * (1.0 - dispersionStrength);

        // Sample base gradient (Color 1 -> Color 2) using warped UVs
        // Using radial distance for a "lens" feel
        float dR = length(uvR - 0.5);
        float dG = length(uvG - 0.5);
        float dB = length(uvB - 0.5);

        vec3 cR = mix(uColor1, uColor2, smoothstep(0.0, 1.0, dR * 1.5));
        vec3 cG = mix(uColor1, uColor2, smoothstep(0.0, 1.0, dG * 1.5));
        vec3 cB = mix(uColor1, uColor2, smoothstep(0.0, 1.0, dB * 1.5));

        // 4. Spectral Interference (Rainbows)
        // High frequency bands that shift hue based on viewing angle (simulated by UV)
        float interference = sin(dR * 20.0 - time * 2.0);
        vec3 spectrum = hueShift(uColor5, interference * 2.0); // Cycle through full spectrum

        // Mask interference to edges of the "glass"
        float prismMask = smoothstep(0.3, 0.8, abs(glassNoise));

        // Compose Channels
        color.r = cR.r + spectrum.r * prismMask * 0.6;
        color.g = cG.g + spectrum.g * prismMask * 0.6;
        color.b = cB.b + spectrum.b * prismMask * 0.6;

        // Add "Caustic" highlights (Color 3)
        float caustic = pow(max(0.0, snoise(uv * 5.0 - vec2(time * 0.2))), 5.0);
        color += uColor3 * caustic * 0.8;

        // Soft Glow (Color 4)
        color += uColor4 * 0.2;

    } else if (uMode > 6.5) {
        // --- FRACTAL MODE (7) - Kaleidoscope + Orbit Traps Hybrid ---

        // Step 1: 8-sided kaleidoscope folding BEFORE fractal iteration
        // This creates the trippy mandala/symmetric vibe
        vec2 fP = uv - 0.5;
        float fA = atan(fP.y, fP.x);
        float fR = length(fP);
        float fSides = 8.0;
        float fTau = 6.2831853;
        fA = mod(fA, fTau / fSides);
        fA = abs(fA - fTau / fSides / 2.0);
        vec2 z = fR * vec2(cos(fA), sin(fA)) * 2.2; // folded + zoomed

        // Step 2: Lissajous animated Julia constant (varies shape over time)
        vec2 jc = vec2(sin(time * 0.25) * 0.7885, cos(time * 0.33) * 0.7885);

        // Step 3: 32-iteration loop with orbit trap tracking
        float fIter = 0.0;
        float trapCircle = 1e6;  // distance to circle r=0.5
        float trapCross = 1e6;   // distance to axes
        float trapOrigin = 1e6;  // distance to origin

        for (int i = 0; i < 32; i++) {
            // z = z^2 + c (Julia iteration)
            z = vec2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y) + jc;

            // Track orbit traps — minimum distance to geometric shapes
            trapCircle = min(trapCircle, abs(length(z) - 0.5));
            trapCross = min(trapCross, min(abs(z.x), abs(z.y)));
            trapOrigin = min(trapOrigin, dot(z, z));

            if (dot(z, z) > 256.0) break;
            fIter += 1.0;
        }

        // Step 4: Smooth escape-time coloring (anti-aliased bands)
        float smoothIter = fIter - log2(log2(dot(z, z))) + 4.0;
        float fNorm = smoothIter / 32.0;

        // Step 5: Map orbit traps to all 5 user colors
        // Each trap shape highlights different structures in the fractal
        vec3 cTrapCircle = uColor1 * exp(-trapCircle * 5.0);
        vec3 cTrapCross = uColor2 * exp(-trapCross * 8.0);
        vec3 cTrapOrigin = uColor3 * exp(-sqrt(trapOrigin) * 3.0);
        vec3 cEscape = mix(uColor4, uColor5, fract(fNorm * 3.0));

        // Blend based on whether point escaped
        float escaped = step(32.0, fIter + 0.5); // 0 if escaped, 1 if trapped
        vec3 trapColor = cTrapCircle + cTrapCross + cTrapOrigin;
        vec3 escapeColor = cEscape * (0.5 + 0.5 * sin(fNorm * 12.0 + time));

        color = mix(escapeColor, trapColor, escaped * 0.7 + 0.3);
        // Add neon glow at fractal boundary
        color += uColor5 * smoothstep(0.8, 1.0, fNorm) * 0.5;
    } else {
        color = vec3(0.0);
    }

    // Vibrance boost — prevent muddy midtones from linear RGB blending
    color = vibranceBoost(color, 0.3);

    // Global Noise/Grain (Dithering)
    float finalNoiseStrength = uNoiseStrength;
    if (uMode > 1.5 && uMode < 2.5) { // Grainy Mode gets extra film grain on top of stipple
        finalNoiseStrength = 0.12;
    }

    float ign = fract(52.9829189 * fract(dot(gl_FragCoord.xy, vec2(0.06711056, 0.00583715))));
    float grain = fract(sin(dot(uv, vec2(12.9898, 78.233))) * 43758.5453);

    color += (grain - 0.5) * finalNoiseStrength;
    color += (ign - 0.5) / 255.0;

    gl_FragColor = vec4(color, 1.0);
}
`,Lt={mesh:0,aurora:1,grainy:2,"deep-sea":3,holographic:4,impasto:5,spectral:6,fractal:7};class he{constructor(e){O(this,"container");O(this,"renderer");O(this,"gl");O(this,"program");O(this,"mesh");O(this,"animationId",0);O(this,"time",0);O(this,"speed",1);O(this,"resizeObserver");if(typeof e.container=="string"){const n=document.querySelector(e.container);if(!n)throw new Error(`Container not found: ${e.container}`);this.container=n}else this.container=e.container;this.renderer=new ke({alpha:!1,dpr:Math.min(window.devicePixelRatio,2),preserveDrawingBuffer:!0}),this.gl=this.renderer.gl,this.container.appendChild(this.gl.canvas),this.gl.canvas.style.width="100%",this.gl.canvas.style.height="100%",this.gl.canvas.style.display="block";const i=new Rt(this.gl);this.program=new ze(this.gl,{vertex:kt,fragment:Ot,uniforms:{uTime:{value:0},uColor1:{value:new T(0,0,0)},uColor2:{value:new T(0,0,0)},uColor3:{value:new T(0,0,0)},uColor4:{value:new T(0,0,0)},uColor5:{value:new T(0,0,0)},uNoiseStrength:{value:.2},uMode:{value:0}}}),this.mesh=new Tt(this.gl,{geometry:i,program:this.program}),this.updateUniforms(e.colors,e.mode,e.noiseStrength),this.resize=this.resize.bind(this),this.resizeObserver=new ResizeObserver(this.resize),this.resizeObserver.observe(this.container),this.resize(),this.update=this.update.bind(this),this.start()}updateUniforms(e,i,n){const r=[...e.map(a=>this.toRgb(a))];for(;r.length<5;)r.push([0,0,0]);const l=this.program.uniforms;l.uColor1.value.set(r[0][0],r[0][1],r[0][2]),l.uColor2.value.set(r[1][0],r[1][1],r[1][2]),l.uColor3.value.set(r[2][0],r[2][1],r[2][2]),l.uColor4.value.set(r[3][0],r[3][1],r[3][2]),l.uColor5.value.set(r[4][0],r[4][1],r[4][2]),l.uNoiseStrength.value=n,l.uMode.value=Lt[i]||0}toRgb(e){return Array.isArray(e)?e:this.hexToRgb(e)}hexToRgb(e){e=e.replace("#","");const i=parseInt(e.substring(0,2),16)/255,n=parseInt(e.substring(2,4),16)/255,s=parseInt(e.substring(4,6),16)/255;return[i,n,s]}resize(){this.container&&this.renderer.setSize(this.container.clientWidth,this.container.clientHeight)}setSpeed(e){this.speed=e}update(){this.animationId=requestAnimationFrame(this.update),this.time+=.01*this.speed,this.program.uniforms.uTime.value=this.time,this.renderer.render({scene:this.mesh})}start(){this.animationId||this.update()}stop(){this.animationId&&(cancelAnimationFrame(this.animationId),this.animationId=0)}dispose(){this.stop(),this.resizeObserver.disconnect(),this.container&&this.gl.canvas.parentNode===this.container&&this.container.removeChild(this.gl.canvas)}}function oe(t,e={}){if(typeof window>"u")return;const i={event:"lumina_event",event_name:t,...e},n=window;if(Array.isArray(n.dataLayer)&&n.dataLayer.push(i),typeof n.clarity=="function")try{n.clarity("event",t)}catch{}}window.LuminaGradient={init:t=>{const e=new he(t);return oe("embed_init",{source:"embed",kind:"manual",mode:t.mode||"mesh"}),e}},document.addEventListener("DOMContentLoaded",()=>{document.querySelectorAll("[data-lumina-gradient]").forEach(e=>{const i=e.getAttribute("data-colors"),n=e.getAttribute("data-mode"),s=e.getAttribute("data-noise"),r=e.getAttribute("data-speed");if(i)try{const l=JSON.parse(i),a=n||"mesh",h=new he({container:e,colors:l,mode:a,noiseStrength:s?parseFloat(s):.2});r&&h.setSpeed(parseFloat(r)),oe("embed_init",{source:"embed",kind:"auto",mode:a})}catch(l){console.error("LuminaGradient: Invalid configuration",l)}})})})();
