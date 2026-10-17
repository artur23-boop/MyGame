import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createMatch,step,push,throwSnow} from '../engine.mjs';
function isolate(m){m.actors.forEach((a,i)=>{a.x=300;a.y=i*20;a.ai=999})}
test('summit scores grounded actors, preserves points off hill, and ends round',()=>{let m=createMatch(1);isolate(m);let p=m.actors[0];p.x=p.y=0;for(let i=0;i<15;i++)step(m,.04);assert.ok(p.score>.5);let score=p.score;p.x=250;step(m,.04);assert.equal(p.score,score);p.x=p.y=0;for(let i=0;i<15;i++)step(m,.04);assert.equal(m.winner,0);let time=m.time;step(m,.04);assert.equal(m.time,time)});
test('stunned and airborne actors cannot score',()=>{let m=createMatch();isolate(m);let p=m.actors[0];p.x=p.y=0;p.stun=1;step(m,.04);assert.equal(p.score,0);p.stun=0;p.jump=.5;step(m,.04);assert.equal(p.score,0)});
test('push requires facing, range and cooldown',()=>{let m=createMatch(),p=m.actors[0],b=m.actors[1];p.x=p.y=0;p.angle=0;b.x=40;b.y=0;assert.equal(push(m,p),true);assert.ok(b.vx>0);assert.ok(b.stun>0);assert.equal(push(m,p),false);p.push=0;b.stun=0;b.vx=0;p.angle=Math.PI;push(m,p);assert.equal(b.vx,0)});
test('snowball hits opponent, stuns and consumes single ammo',()=>{let m=createMatch();isolate(m);let p=m.actors[0],b=m.actors[1];p.x=-160;p.y=0;p.angle=0;b.x=-80;b.y=0;assert.ok(throwSnow(m,p));assert.equal(throwSnow(m,p),false);for(let i=0;i<5;i++)step(m,.04);assert.ok(b.stun>1);assert.equal(m.shots.length,0)});
test('bots reach hill and complete a round without input',()=>{let m=createMatch(3,'normal',()=>.5);for(let i=0;i<15000&&m.winner===null;i++)step(m,.04);assert.notEqual(m.winner,null);assert.ok(m.actors[m.winner].score>=3)});
test('dash has cooldown and arena keeps actors inside',()=>{let m=createMatch();isolate(m);let p=m.actors[0];step(m,.04,{x:1,dash:true});assert.equal(p.dash,3);step(m,.04,{x:1,dash:true});assert.ok(p.dash<3);for(let i=0;i<400;i++)step(m,.04,{x:1});assert.ok(Math.hypot(p.x,p.y)<=365.001)});
