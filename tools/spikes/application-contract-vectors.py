#!/usr/bin/env python3
"""Architecture oracles only. Run in a disposable venv with blake3==1.0.8 rfc8785==0.1.4.
Exact rational Bernstein bounds exercise a conservative admitted subset; this is not a FoilDSL parser.
"""
import base64
from fractions import Fraction as F
import hashlib
import heapq
import json
import math
import os
from pathlib import Path
import struct
import subprocess
import sys
import tempfile
import time

import blake3
import rfc8785

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def split(coeff):
    levels = [list(coeff)]
    while len(levels[-1]) > 1:
        row = levels[-1]
        levels.append([(a+b)/2 for a, b in zip(row, row[1:])])
    return [row[0] for row in levels], [row[-1] for row in levels[::-1]]


def de_boor(points, knots, degree, t):
    if t == knots[-1]:
        return points[-1]
    k = next(i for i in range(degree, len(points)) if knots[i] <= t < knots[i+1])
    work = list(points[k-degree:k+1])
    for r in range(1, degree+1):
        for j in range(degree, r-1, -1):
            i = k-degree+j
            den = knots[i+degree-r+1]-knots[i]
            a = (t-knots[i])/den if den else F(0)
            work[j] = (1-a)*work[j-1]+a*work[j]
    return work[degree]


def bernstein_spans(points, knots, degree):
    """Exact rational collocation recovers each polynomial span; no floating power conversion."""
    out = []
    for left, right in zip(knots, knots[1:]):
        if left == right:
            continue
        matrix = []
        for j in range(degree+1):
            u = F(j, degree)
            matrix.append([F(math.comb(degree, i))*u**i*(1-u)**(degree-i) for i in range(degree+1)] + [de_boor(points, knots, degree, left+(right-left)*u)])
        for col in range(degree+1):
            pivot = next(row for row in range(col, degree+1) if matrix[row][col])
            matrix[col], matrix[pivot] = matrix[pivot], matrix[col]
            divisor = matrix[col][col]
            matrix[col] = [v/divisor for v in matrix[col]]
            for row in range(degree+1):
                if row != col:
                    factor = matrix[row][col]
                    matrix[row] = [a-factor*b for a,b in zip(matrix[row],matrix[col])]
        out.append((left,right,[row[-1] for row in matrix]))
    return out


def positive_open(coeff):
    # Every Bernstein basis is strictly positive on the open interval.
    return min(coeff) >= 0 and max(coeff) > 0


def increasing_on_every_span(x, knots, degree):
    spans = bernstein_spans(x, knots, degree)
    for _,_,coeff in spans:
        derivative = [degree*(b-a) for a,b in zip(coeff,coeff[1:])]
        if not positive_open(derivative):
            return False
    return True


def chord_separation(leading, leading_knots, trailing, trailing_knots):
    leading_spans=bernstein_spans(leading,leading_knots,3)
    trailing_spans=bernstein_spans(trailing,trailing_knots,3)
    trailing_floor=min(min(c) for _,_,c in trailing_spans)
    return [trailing_floor-max(c) for _,_,c in leading_spans]


def maximum_enclosure(coeff, tolerance=F(1,10**10), budget=4096):
    queue=[]; serial=0
    def put(c):
        nonlocal serial
        serial+=1
        heapq.heappush(queue,(-max(c),serial,c))
    put(coeff)
    lower=max(coeff[0],coeff[-1])
    for count in range(budget):
        upper=-queue[0][0]
        if upper-lower <= tolerance:
            return lower,upper,count
        _,_,c=heapq.heappop(queue)
        a,b=split(c)
        lower=max(lower,a[-1])
        put(a);put(b)
    return None  # Not assessed, never accept on exhausted work.


def check(name, condition, detail=None):
    if not condition:
        raise AssertionError(name)
    return {"name":name,"pass":True,"detail":detail}


def hash_bytes(data):
    return hashlib.sha256(data).hexdigest()


def atomic_spike(path, data, expected, phase=None):
    """Cooperative single-writer protocol; does not claim CAS against uncooperative writers."""
    if any(p.is_symlink() for p in [path,*path.parents]):
        raise ValueError("DOC-PATH")
    lock=path.with_suffix('.lock'); temp=path.with_suffix('.temporary')
    fd=os.open(lock,os.O_CREAT|os.O_EXCL|os.O_WRONLY,0o600)
    temp_created=False
    try:
        os.close(fd)
        current=hash_bytes(path.read_bytes()) if path.exists() else None
        if current != expected:
            raise ValueError("DOC-CONFLICT")
        if phase=='before-write': raise OSError('injected')
        fd=os.open(temp,os.O_CREAT|os.O_EXCL|os.O_WRONLY|os.O_NOFOLLOW,0o600)
        temp_created=True
        with os.fdopen(fd,'wb') as output:
            output.write(data[:len(data)//2])
            if phase=='partial-write': raise OSError('injected')
            output.write(data[len(data)//2:]);output.flush();os.fsync(output.fileno())
        if phase=='external-change': path.write_bytes(b'external-B')
        if (hash_bytes(path.read_bytes()) if path.exists() else None) != expected:
            raise ValueError("DOC-CONFLICT")
        if phase=='before-replace': raise OSError('injected')
        os.replace(temp,path)
        directory=os.open(path.parent,os.O_RDONLY)
        try: os.fsync(directory)
        finally: os.close(directory)
        if phase=='after-replace': raise OSError('injected')
    finally:
        if temp_created: temp.unlink(missing_ok=True)
        lock.unlink(missing_ok=True)


def run(output_dir, native=None):
    start=time.perf_counter(); checks=[]; vectors=[]
    for decimal,scale in [('14.049',F(1,1000)),('1.4049',F(1,100)),('0.014049',F(1)),('1e-7',F(1)),('5e-324',F(1))]:
        value=float(F(decimal)*scale)
        canonical=rfc8785.dumps(value)
        vectors.append({'input':decimal,'scale':str(scale),'binary64':struct.pack('>d',value).hex(),'jcs':canonical.decode(),'blake3':blake3.blake3(canonical).hexdigest()})
    checks.append(check('Decimal_ScaledExact_FirstThreeBitsEqual',len({v['binary64'] for v in vectors[:3]})==1,vectors[:3]))
    obj={'z':-0.0,'a':[1e-7,1e21,0.014049],'text':'é'}
    canonical=rfc8785.dumps(obj)
    checks.append(check('Jcs_ExponentNegativeZeroUnicode_ExactBytes',canonical==b'{"a":[1e-7,1e+21,0.014049],"text":"\xc3\xa9","z":0}'))
    checks.append(check('Blake3_Empty_OfficialVector',blake3.blake3(b'').hexdigest()=='af1349b9f5f9a1a6a0404dea36dcc9499bcb25c9adc112b7cc9a93cae41f3262'))
    checks.append(check('Source_Trivia_SeparateIdentity',hash_bytes(b'a\r\n')!=hash_bytes(b'a\n')))
    canonical_path=output_dir/'canonical.json';canonical_path.write_bytes(canonical)
    if native:
        actual=json.loads(subprocess.check_output([native,'--hash',str(canonical_path)],text=True,encoding='utf-8',errors='replace'))
        checks.append(check('CSharpPython_SameCanonicalBytes_SameDigests',actual=={'sha256':hash_bytes(canonical),'blake3':blake3.blake3(canonical).hexdigest()},actual))
    knots=list(map(F,[0,0,0,0,.25,.75,1,1,1,1]))
    x=list(map(F,[0,.125,.375,.625,.875,1]))
    checks.append(check('Derivative_EveryOpenKnotSpan_StrictlyIncreasing',increasing_on_every_span(x,knots,3)))
    repeated=list(map(F,[0,0,0,0,.5,.5,1,1,1,1]))
    checks.append(check('Derivative_RepeatedInteriorKnot_AllNonemptySpans',increasing_on_every_span(x,repeated,3)))
    checks.append(check('Derivative_FlatCurve_NotAssessed',not increasing_on_every_span([F(0)]*6,knots,3)))
    checks.append(check('Derivative_BoundaryRepeatedRoot_PositiveOpenSupport',positive_open([F(0),F(0),F(1)])))
    checks.append(check('Derivative_InteriorRepeatedRoot_ConservativeNotAssessed',not positive_open([F(1,4),F(-1,4),F(1,4)])))
    leading=[F(0),F(0),F(1,100),F(1,50),F(3,100),F(3,100)]
    trailing=[F(1,10),F(1,10),F(13,100),F(12,100),F(1,10),F(1,10)]
    rail_bounds=[(min(c),max(c)) for _,_,c in bernstein_spans(leading,knots,3)]
    margins=chord_separation(leading,knots,trailing,repeated)
    checks.append(check('Chord_IndependentBases_PositiveEveryOpenSpan',all(m>0 for m in margins),list(map(str,margins))))
    near=chord_separation([F(0),F(0),F(1,10),F(1,10),F(1,10),F(1,10)],knots,[F(1,10)+F(1,10**12)]*6,repeated)
    touching=chord_separation([F(0),F(0),F(1,10),F(1,10),F(1,10),F(1,10)],knots,[F(1,10)]*6,repeated)
    checks.append(check('Chord_NearBound_AdmitsPositiveRefusesTouching',min(near)==F(1,10**12) and min(touching)==0))
    checks.append(check('Chord_TouchOrNegativeCoefficient_FailClosed',not positive_open([F(0)]*4) and not positive_open([F(1),F(-1),F(1)])))
    thickness=list(map(F,[0,.08,.12,.12,.06,0]))
    checks.append(check('Profile_ClosedEndpoints_StrictInteriorThickness',positive_open(thickness)))
    bound=maximum_enclosure(thickness)
    checks.append(check('Thickness_Maximum_EnclosedNormalization',bound is not None and bound[0]>0 and bound[1]-bound[0]<=F(1,10**10),[str(v) for v in bound]))
    checks.append(check('Thickness_BudgetExhausted_NotAssessed',maximum_enclosure(thickness,budget=0) is None))
    checks.append(check('Thickness_UnsupportedCrossing_NotAccepted',not positive_open([F(0),F(1),F(-1),F(0)])))
    # Since max is enclosed in [lo,hi], normalized ordinates use interval division, never a guessed midpoint certificate.
    norm_low=F(9,100)/bound[1];norm_high=F(9,100)/bound[0]
    maximum_known=maximum_enclosure([F(i*(5-i),20) for i in range(6)]) # t(1-t), exact max=1/4.
    checks.append(check('Maximum_KnownQuadraticDegreeElevated_EnclosesQuarter',maximum_known[0]<=F(1,4)<=maximum_known[1]))
    # Thickness-only uncertainty in q=(x,C +/- tc*T/2): fixed camber C cancels.
    # This excludes inversion, trigonometric and final rounding error.
    physical_error=F(2)*max(thickness)*(norm_high-norm_low)/2 # chord <=2 m, either section side.
    checks.append(check('Normalization_EnclosurePropagated_PhysicalErrorBelowOneNanometre',0<=physical_error<F(1,10**9),{'scaleLower':str(norm_low),'scaleUpper':str(norm_high),'sideErrorMetresUpper':str(physical_error)}))
    source=b'foildsl "4.0" # exact\r\n'
    envelope={'format':'cfdw-project-1','projectId':'fixture','revisions':[{'id':'r1','parent':None,'sourceUtf8Base64':base64.b64encode(source).decode(),'sourceSha256':hash_bytes(source)}],'activeRevision':'r1','recovery':{'base':'r1','sourceUtf8Base64':base64.b64encode(b'incomplete {').decode()}}
    data=json.dumps(envelope,ensure_ascii=False,indent=2).encode()
    path=output_dir/'project.cfdw.json';original=b'original-A'
    atomic_spike(path,data,None)
    checks.append(check('Atomic_NewFile_ExpectedAbsentCreatesCompleteEnvelope',path.read_bytes()==data))
    path.write_bytes(original)
    preexisting_temp=path.with_suffix('.temporary');preexisting_temp.write_bytes(b'owned-by-other')
    try:atomic_spike(path,data,hash_bytes(original))
    except FileExistsError:pass
    else:raise AssertionError('Existing temporary accepted')
    checks.append(check('Atomic_TemporaryCollision_PreservesUnownedFileTargetAndReleasesClaim',preexisting_temp.read_bytes()==b'owned-by-other' and path.read_bytes()==original and not path.with_suffix('.lock').exists()))
    preexisting_temp.unlink()
    existing_claim=path.with_suffix('.lock');existing_claim.write_bytes(b'other-writer')
    try:atomic_spike(path,data,hash_bytes(original))
    except FileExistsError:pass
    else:raise AssertionError('Existing writer claim accepted')
    checks.append(check('Atomic_ClaimCollision_PreservesOtherClaimAndTarget',existing_claim.read_bytes()==b'other-writer' and path.read_bytes()==original and not preexisting_temp.exists()))
    existing_claim.unlink()
    for fault in ['before-write','partial-write','before-replace','after-replace']:
        path.write_bytes(original)
        try:atomic_spike(path,data,hash_bytes(original),fault)
        except OSError:pass
        checks.append(check('Atomic_'+fault+'_OldOrCompleteNew',path.read_bytes()==(data if fault=='after-replace' else original)))
    path.write_bytes(original)
    try:atomic_spike(path,data,hash_bytes(original),'external-change')
    except ValueError as e:checks.append(check('Atomic_ExternalChange_ConflictPreservesB',str(e)=='DOC-CONFLICT' and path.read_bytes()==b'external-B'))
    else:raise AssertionError('External conflict accepted')
    path.write_bytes(original);atomic_spike(path,data,hash_bytes(original))
    loaded=json.loads(path.read_bytes())
    checks.append(check('Reopen_RecoverySeparate_AcceptedSourceExact',base64.b64decode(loaded['revisions'][0]['sourceUtf8Base64'])==source and loaded['recovery']['base']=='r1'))
    link=output_dir/'link.cfdw.json';link.symlink_to(path)
    try:atomic_spike(link,data,hash_bytes(data))
    except ValueError as e:checks.append(check('Atomic_Symlink_Refused',str(e)=='DOC-PATH'))
    else:raise AssertionError('Symlink accepted')
    ancestor=output_dir/'ancestor';ancestor.symlink_to(output_dir,target_is_directory=True)
    try:atomic_spike(ancestor/path.name,data,hash_bytes(data))
    except ValueError as e:checks.append(check('Atomic_AncestorSymlink_Refused',str(e)=='DOC-PATH'))
    else:raise AssertionError('Ancestor symlink accepted')
    report={'checks':checks,'vectors':vectors,'canonicalUtf8':canonical.decode(),'canonicalSha256':hash_bytes(canonical),'canonicalBlake3':blake3.blake3(canonical).hexdigest(),'seconds':time.perf_counter()-start,'limits':['No FoilDSL parser','No C# JCS implementation','Conservative geometry sufficient subset only','Uncooperative writer after final hash check remains a race','No Windows live or power-loss proof']}
    print(json.dumps(report,indent=2))
    return report


if __name__=='__main__':
    scratch=Path(sys.argv[1]).resolve();scratch.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='contracts-',dir=scratch) as directory:
        run(Path(directory),sys.argv[2] if len(sys.argv)>2 else None)
