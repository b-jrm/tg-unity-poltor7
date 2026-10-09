import math, os, json, random
from PIL import Image, ImageDraw, ImageFont

SS = 4
OUTLINE = (11, 18, 32, 255)
rad = math.radians

def hexc(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (255,)

def sh(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c[:3]) + (255,)

def mix(a, b, t):
    return tuple(int(a[i] * (1 - t) + b[i] * t) for i in range(3)) + (255,)


class Cv:
    def __init__(self, cell):
        self.cell = cell
        self.s = cell / 32.0
        self.img = Image.new('RGBA', (cell * SS, cell * SS), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.img)

    def P(self, x, y):
        k = self.s * SS
        return (x * k, y * k)

    def poly(self, pts, col):
        self.d.polygon([self.P(*p) for p in pts], fill=col)

    def rect(self, x0, y0, x1, y1, col):
        a, b = self.P(x0, y0), self.P(x1, y1)
        self.d.rectangle([a, (b[0] - 1, b[1] - 1)], fill=col)

    def ell(self, cx, cy, rx, ry, col):
        a, b = self.P(cx - rx, cy - ry), self.P(cx + rx, cy + ry)
        self.d.ellipse([a, b], fill=col)

    def line(self, pts, w, col):
        k = self.s * SS
        pp = [self.P(*p) for p in pts]
        self.d.line(pp, fill=col, width=max(1, int(w * k)), joint='curve')
        r = w * k / 2
        for p in (pp[0], pp[-1]):
            self.d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=col)

    def limb(self, x, y, L, ang, w, col):
        a = rad(ang)
        dx, dy = math.sin(a), math.cos(a)
        px, py = math.cos(a), -math.sin(a)
        h = w / 2
        ex, ey = x + dx * L, y + dy * L
        self.poly([(x + px * h, y + py * h), (ex + px * h, ey + py * h), (ex - px * h, ey - py * h), (x - px * h, y - py * h)], col)
        self.ell(x, y, h, h, col)
        self.ell(ex, ey, h, h, col)
        return ex, ey

    def arc(self, cx, cy, r, a0, a1, w, col):
        k = self.s * SS
        c = self.P(cx, cy)
        rr = r * k
        self.d.arc([c[0] - rr, c[1] - rr, c[0] + rr, c[1] + rr], a0, a1, fill=col, width=max(1, int(w * k)))

    def star(self, x, y, r, col, col2=None):
        pts = []
        for i in range(8):
            rr = r if i % 2 == 0 else r * 0.45
            a = math.pi * 2 * i / 8
            pts.append((x + rr * math.cos(a), y + rr * math.sin(a)))
        self.poly(pts, col)
        if col2:
            self.ell(x, y, r * 0.35, r * 0.35, col2)

    def sparks(self, seed, n, cx, cy, spread, cols):
        rnd = random.Random(seed)
        for _ in range(n):
            x = cx + rnd.uniform(-spread, spread)
            y = cy + rnd.uniform(-spread, spread)
            self.rect(x, y, x + 0.9, y + 0.9, rnd.choice(cols))

    def finish(self, flash=0.0, rot=0.0, pivot=(16, 30), dx=0, dy=0, squash=1.0, tint=None, outline=True):
        img = self.img
        k = self.s * SS
        if rot or dx or dy:
            img = img.rotate(rot, resample=Image.NEAREST, center=(pivot[0] * k, pivot[1] * k), translate=(dx * k, dy * k))
        if squash < 1.0:
            hgt = img.size[1]
            ny = int(hgt * squash)
            sq = img.resize((img.size[0], ny), Image.NEAREST)
            canvas = Image.new('RGBA', img.size, (0, 0, 0, 0))
            base = int(30.6 * k)
            canvas.paste(sq, (0, base - int(base * squash)))
            img = canvas
        out = img.resize((self.cell, self.cell), Image.NEAREST)
        px = out.load()
        W = H = self.cell
        for y in range(H):
            for x in range(W):
                r, g, b, a = px[x, y]
                if a < 128:
                    px[x, y] = (0, 0, 0, 0)
                else:
                    px[x, y] = (r, g, b, 255)
        res = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        rp = res.load()
        def solid(x, y):
            return 0 <= x < W and 0 <= y < H and px[x, y][3] > 0
        for y in range(H):
            for x in range(W):
                if px[x, y][3] == 0:
                    continue
                r, g, b, _ = px[x, y]
                up, lf = not solid(x, y - 1), not solid(x - 1, y)
                dn, rt = not solid(x, y + 1), not solid(x + 1, y)
                if up or lf:
                    r, g, b = [min(255, int(v * 1.16 + 8)) for v in (r, g, b)]
                elif dn or rt:
                    r, g, b = [int(v * 0.80) for v in (r, g, b)]
                if tint:
                    r, g, b = [int(v * (1 - tint[3]) + tint[i] * tint[3]) for i, v in enumerate((r, g, b))]
                if flash:
                    r, g, b = [int(v * (1 - flash) + 255 * flash) for v in (r, g, b)]
                rp[x, y] = (r, g, b, 255)
        if outline:
            for y in range(H):
                for x in range(W):
                    if px[x, y][3] == 0 and (solid(x - 1, y) or solid(x + 1, y) or solid(x, y - 1) or solid(x, y + 1)):
                        rp[x, y] = OUTLINE
        return res


# ------------------------------------------------------------------ HUMANOID / ROBOT RIG
FOOT = 1.2

def rig(cv, p, st):
    kind = st['kind']
    bulk = st.get('bulk', 1.0)
    L1, L2 = st['thigh'], st['shin']
    hx = 16 + p.get('hip_x', 0)
    hy0 = st['hip_y']

    def ends(hxx, hyy, leg):
        th, sn = leg
        kx = hxx + L1 * math.sin(rad(th)); ky = hyy + L1 * math.cos(rad(th))
        ex = kx + L2 * math.sin(rad(sn)); ey = ky + L2 * math.cos(rad(sn))
        return kx, ky, ex, ey

    eF = ends(hx + 0.9, hy0, p['legF']); eB = ends(hx - 0.9, hy0, p['legB'])
    shift = 30.4 - (max(eF[3], eB[3]) + FOOT)
    hy = hy0 + shift + p.get('dy', 0)
    lean = p.get('lean', 0)
    tw = st['torso_h']
    sy = hy - tw
    sx = hx + lean
    C = st['c']
    wl = st['leg_w'] * bulk
    wa = st['arm_w'] * bulk

    def leg(hxx, leg_, col, bootc):
        th, sn = leg_
        kx = hxx + L1 * math.sin(rad(th)); ky = hy + L1 * math.cos(rad(th))
        cv.limb(hxx, hy, L1, th, wl, col)
        ex, ey = cv.limb(kx, ky, L2, sn, wl * 0.92, col)
        if kind == 'robot':
            cv.rect(ex - 2.0 * bulk, ey - 0.8, ex + 3.2 * bulk, ey + FOOT, bootc)
            cv.rect(ex - 0.8, ey - 2.6, ex + 0.8, ey - 0.8, sh(col, 0.8))
        else:
            cv.poly([(ex - 1.3 * bulk, ey - 1.0), (ex + 2.9 * bulk, ey - 0.4), (ex + 3.1 * bulk, ey + FOOT), (ex - 1.3 * bulk, ey + FOOT)], bootc)

    def arm(shx, shy, a, col, hand, front):
        up, fo = a
        shy2 = shy + 1.0
        ex, ey = cv.limb(shx, shy2, st['arm1'], up, wa, col)
        hxe, hye = cv.limb(ex, ey, st['arm2'], fo, wa * 0.9, col)
        if kind == 'robot':
            cv.ell(hxe, hye, 1.9 * bulk, 1.9 * bulk, hand)
        else:
            cv.ell(hxe, hye, 1.2 * bulk, 1.2 * bulk, hand)
        return hxe, hye, fo

    # ---- cuerpo trasero
    backc = sh(C['body'], 0.62)
    legB_c = sh(C['legs'], 0.7)
    armB_c = sh(C['arm'], 0.65)
    if kind == 'vex':
        cv.ell(sx - 3.3 * bulk, sy + 1.2, 2.8, 2.6, sh(C['pad'], 0.7))
    hb = arm(sx - 0.8, sy, p['armB'], armB_c, sh(C['hand'], 0.75), False)
    if p.get('weapon') in ('rifle', 'cannon') and kind in ('guardia',):
        pass
    leg(hx - 0.9, p['legB'], legB_c, sh(C['boot'], 0.75))

    # ---- torso
    tt = st['torso_w'] * bulk
    tb = st['torso_wb'] * bulk
    pts = [(sx - tt / 2, sy), (sx + tt / 2, sy), (hx + tb / 2, hy + 0.4), (hx - tb / 2, hy + 0.4)]
    if kind == 'mara':
        coat = C['body']
        sway = p.get('hem', 0)
        cv.poly([(sx - 3.2, sy), (sx + 3.2, sy), (hx + 4.3 + sway, hy + 6.8), (hx - 4.3 + sway, hy + 6.8)], coat)
        cv.poly([(sx - 0.5, sy + 0.5), (sx + 1.2, sy + 0.5), (hx + 1.2, hy + 6.5), (hx - 0.5, hy + 6.5)], sh(coat, 0.9))
        cv.rect(sx + 0.8, sy + 2.2, sx + 2.0, sy + 3.0, hexc('e03131'))
        cv.rect(sx + 1.2, sy + 1.8, sx + 1.6, sy + 3.4, hexc('e03131'))
    else:
        cv.poly(pts, C['body'])
        if kind == 'ike':
            cv.rect(sx - tt / 2, sy + 0.1, sx + tt / 2, sy + 1.6, C['trim'])
            cv.rect(hx - tb / 2, hy - 1.3, hx + tb / 2, hy + 0.5, C['belt'])
            cv.rect(hx + 0.6, hy - 1.2, hx + 2.6, hy + 1.8, sh(C['belt'], 1.3))
            cv.rect(sx - 1.0, sy + 3.0, sx + 1.4, sy + 4.0, C['trim'])
        elif kind == 'raizal':
            cv.poly([(hx - tb / 2, hy + 0.4), (hx + tb / 2, hy + 0.4), (hx + tb / 2 - 0.3, hy + 2.2), (hx + 0.8, hy + 1.2), (hx - 0.6, hy + 2.6), (hx - tb / 2, hy + 1.4)], C['legs'])
            for (ox, oy) in ((-1.4, 2.0), (1.4, 4.2), (0.2, 6.0)):
                cv.ell(sx + ox, sy + oy, 0.9, 0.9, C['glow'])
            cv.rect(sx - tt / 2, sy + 0.5, sx - tt / 2 + 1.4, sy + 3, sh(C['skin'], 1.0))
        elif kind == 'guardia':
            cv.rect(sx - 2.2, sy + 1.2, sx + 2.6, sy + 5.0, C['plate'])
            cv.rect(hx - tb / 2, hy - 1.2, hx + tb / 2, hy + 0.4, sh(C['body'], 0.6))
            cv.rect(sx + 0.2, sy + 2.4, sx + 1.6, sy + 3.0, C['accent'])
        elif kind == 'vex':
            cv.rect(hx - tb / 2, hy - 1.4, hx + tb / 2, hy + 0.5, sh(C['body'], 0.6))
            cv.ell(sx + 0.6, sy + 3.6, 1.7, 2.0, C['crystal'])
            cv.ell(sx + 0.6, sy + 3.6, 0.8, 1.0, mix(C['crystal'], (255, 255, 255), 0.7))
            cv.rect(sx - tt / 2 + 0.6, sy + 6.0, sx + tt / 2 - 0.6, sy + 6.5, C['accent'])
        elif kind == 'robot':
            cv.rect(sx - tt / 2 + 1.0, sy + 1.2, sx + tt / 2 - 1.0, sy + 6.2, C['plate'])
            for yy in (2.2, 3.4, 4.6):
                cv.rect(sx - 1.2, sy + yy, sx + 2.2, sy + yy + 0.5, sh(C['plate'], 0.55))
            cv.ell(sx - tt / 2 + 1.0, sy + 1.4, 0.5, 0.5, C['accent'])
            cv.rect(hx - tb / 2, hy - 1.4, hx + tb / 2, hy + 0.4, sh(C['body'], 0.6))

    # ---- pierna delantera
    leg(hx + 0.9, p['legF'], C['legs'], C['boot'])

    # ---- cabeza
    hxc = sx + 1.0 + p.get('head_dx', 0)
    hyc = sy - st['neck'] + p.get('head_dy', 0)
    hs = st['head']
    if kind == 'robot':
        cv.rect(hxc - 3.0 * bulk, hyc - 2.2 * bulk, hxc + 3.4 * bulk, hyc + 2.4 * bulk, C['head'])
        cv.rect(hxc - 3.0 * bulk, hyc - 2.6 * bulk, hxc + 1.0 * bulk, hyc - 1.8 * bulk, sh(C['head'], 0.8))
        ec = p.get('eye', C['eye'])
        cv.rect(hxc + 0.4 * bulk, hyc - 0.6, hxc + 3.0 * bulk, hyc + 0.9, ec)
        cv.rect(hxc + 1.8 * bulk, hyc - 0.2, hxc + 2.8 * bulk, hyc + 0.5, mix(ec, (255, 255, 255), 0.7))
        cv.rect(hxc - 0.4, hyc - 3.6 * bulk, hxc + 0.2, hyc - 2.2 * bulk, sh(C['head'], 0.7))
        cv.ell(hxc - 0.1, hyc - 3.8 * bulk, 0.6, 0.6, C['accent'])
    elif kind in ('guardia', 'vex'):
        cv.ell(hxc, hyc, hs[0] * bulk, hs[1] * bulk, C['head'])
        cv.rect(hxc - 0.5, hyc - 0.9, hxc + hs[0] * bulk + 0.1, hyc + 0.9, C['visor'])
        cv.rect(hxc + 0.6, hyc - 0.4, hxc + hs[0] * bulk, hyc + 0.1, mix(C['visor'], (255, 255, 255), 0.6))
        if kind == 'vex':
            cv.poly([(hxc - 2.5, hyc - 2.4), (hxc + 0.5, hyc - 4.6), (hxc + 1.5, hyc - 2.6)], C['accent'])
        cv.rect(hxc - hs[0] * bulk, hyc + 1.2, hxc - 1, hyc + 2.6, sh(C['head'], 0.8))
    else:
        cv.ell(hxc, hyc, hs[0], hs[1], C['skin'])
        if kind == 'ike':
            cv.ell(hxc - 0.8, hyc - 1.1, hs[0] + 0.1, hs[1] - 0.9, C['hair'])
            cv.rect(hxc - hs[0] + 0.4, hyc - 0.9, hxc + hs[0] - 0.2, hyc - 0.1, C['trim'])
            cv.rect(hxc + 0.7, hyc - 0.7, hxc + 2.2, hyc - 0.2, hexc('ffe8b0'))
            cv.rect(hxc + 1.4, hyc + 0.4, hxc + 2.0, hyc + 1.0, hexc('2b1d14'))
        elif kind == 'raizal':
            cv.ell(hxc - 1.4, hyc - 1.4, 1.5, 1.2, sh(C['skin'], 0.7))
            cv.ell(hxc + 1.3, hyc - 0.2, 1.0, 0.9, C['eye'])
            cv.ell(hxc + 1.3, hyc - 0.2, 0.4, 0.4, hexc('ffffff'))
            mo = p.get('mouth', 0)
            cv.rect(hxc + 0.4, hyc + 1.3, hxc + 2.6, hyc + 1.3 + 0.6 + mo, hexc('3a0d0d'))
            cv.ell(hxc - 0.6, hyc + 2.0, 0.7, 0.7, C['glow'])
        elif kind == 'mara':
            cv.ell(hxc - 1.0, hyc - 0.9, hs[0] + 0.1, hs[1] - 0.7, C['hair'])
            tail = p.get('tail', 0)
            cv.poly([(hxc - 2.4, hyc - 1.2), (hxc - 4.8 + tail, hyc + 1.0), (hxc - 4.0 + tail, hyc + 3.8), (hxc - 2.2, hyc + 1.0)], C['hair'])
            cv.rect(hxc + 1.2, hyc - 0.2, hxc + 2.0, hyc + 0.7, hexc('2b1d14'))
            cv.rect(hxc + 0.8, hyc + 1.4, hxc + 1.8, hyc + 1.8, hexc('a24a4a'))

    # ---- brazo delantero + arma
    hxe, hye, fo = arm(sx + 0.4, sy, p['armF'], C['arm'], C['hand'], True)
    w = p.get('weapon')
    tip = None
    if w == 'pistol':
        tip = cv.limb(hxe, hye, 3.6, fo, 1.7, hexc('2a3340'))
        cv.limb(hxe, hye, 1.2, fo, 2.2, hexc('3d4858'))
        cv.limb(tip[0], tip[1], 0.6, fo, 1.0, hexc('ff7a2f'))
    elif w == 'rifle':
        cv.limb(hxe - math.sin(rad(fo)) * 3, hye - math.cos(rad(fo)) * 3, 3.2, fo, 2.2, hexc('3d4858'))
        tip = cv.limb(hxe, hye, 6.4, fo, 1.7, hexc('2a3340'))
        cv.limb(hxe + math.sin(rad(fo)) * 3.4, hye + math.cos(rad(fo)) * 3.4 + 0.1, 0.8, fo - 90, 1.3, hexc('ff3b30'))
    elif w == 'wrench':
        tip = cv.limb(hxe, hye, 4.8, fo, 1.4, hexc('aab4c2'))
        cv.ell(tip[0], tip[1], 1.7, 1.7, hexc('c9d1db'))
        cv.ell(tip[0] + math.sin(rad(fo)) * 0.9, tip[1] + math.cos(rad(fo)) * 0.9, 0.7, 0.7, OUTLINE)
    elif w == 'cannon':
        cv.limb(hxe - math.sin(rad(fo)) * 1.5, hye - math.cos(rad(fo)) * 1.5, 3.0, fo, 4.4, hexc('2a2f3d'))
        tip = cv.limb(hxe + math.sin(rad(fo)) * 1.5, hye + math.cos(rad(fo)) * 1.5, 4.6, fo, 3.0, hexc('1b1f2a'))
        cv.limb(tip[0], tip[1], 0.7, fo, 3.2, C['accent'])
    elif w == 'claw':
        a = fo
        cv.ell(hxe, hye, 2.3 * bulk, 2.3 * bulk, hexc('8a95a5'))
        op = p.get('claw', 20)
        for da in (-op, op):
            cv.limb(hxe, hye, 3.4 * bulk, a + da, 1.5 * bulk, hexc('c9d1db'))
        tip = (hxe + math.sin(rad(a)) * 4, hye + math.cos(rad(a)) * 4)
    elif w == 'claws_f':
        for da in (-14, 0, 14):
            cv.limb(hxe, hye, 2.2, fo + da, 0.9, sh(C['hand'], 1.1))
    if p.get('flash') and tip:
        cv.star(tip[0] + math.sin(rad(fo)) * 1.5, tip[1] + math.cos(rad(fo)) * 1.5, p['flash'], hexc('ffd43b'), hexc('ffffff'))
    if p.get('swoosh'):
        a0, a1, r = p['swoosh']
        cv.arc(sx + 2, sy + 1, r, a0, a1, 1.4, hexc('e8f4ff'))
    if p.get('sparks'):
        cv.sparks(p['sparks'], 7, sx, sy + 4, 7, [hexc('ffd43b'), hexc('ff922b'), hexc('ffffff')])
    if p.get('dust'):
        for i in range(5):
            cv.ell(hx + 5 + i * 1.6, 29.2 - (i % 2) * 0.8, 1.2, 0.9, hexc('c9d1db'))
    return cv


def render_h(cell, st, p):
    cv = Cv(cell)
    rig(cv, p, st)
    kw = dict(flash=p.get('fl', 0))
    if p.get('rot') is not None:
        a = p['rot']
        kw.update(rot=a, pivot=(16, 30), dx=12 * math.sin(rad(a)), dy=-3 * math.sin(rad(a)))
    if p.get('squash'):
        kw['squash'] = p['squash']
    if p.get('tint'):
        kw['tint'] = p['tint']
    return cv.finish(**kw)


# ------------------------------------------------------------------ POSES
def lerp(a, b, t):
    return a + (b - a) * t

def idle_poses(n, armF=(8, 14), armB=(-4, 6), lean=0.3, extra=None):
    out = []
    for i in range(n):
        ph = 2 * math.pi * i / n
        d = dict(legF=(6, 6), legB=(-6, -6), lean=lean + 0.35 * math.sin(ph), head_dy=0.45 * math.sin(ph + 0.7),
                 armF=(armF[0] + 3 * math.sin(ph), armF[1] + 5 * math.sin(ph)),
                 armB=(armB[0] - 3 * math.sin(ph), armB[1] - 4 * math.sin(ph)), hem=0.3 * math.sin(ph), tail=0.5 * math.sin(ph))
        if extra:
            d.update(extra)
        out.append(d)
    return out

def walk_poses(n, amp=30, bend=42, arm=26, lean=0.8, base=(0, 10), extra=None):
    out = []
    for i in range(n):
        ph = 2 * math.pi * i / n
        thF = amp * math.sin(ph)
        bF = bend * max(0, math.cos(ph)); bB = bend * max(0, -math.cos(ph))
        d = dict(legF=(thF, thF - bF), legB=(-thF, -thF - bB), lean=lean,
                 armF=(base[0] - arm * math.sin(ph), base[1] - arm * 0.5 * math.sin(ph)),
                 armB=(base[0] + arm * math.sin(ph), base[1] + arm * 0.5 * math.sin(ph)),
                 head_dy=0.5 * abs(math.sin(ph)) * -1, hem=2.0 * math.sin(ph), tail=1.4 * math.sin(ph + 1))
        if extra:
            d.update(extra)
        out.append(d)
    return out

def die_poses(base, angles=(0, 14, 34, 58, 80, 90), fl=(0.7, 0, 0, 0, 0, 0), extra_each=None):
    out = []
    for i, a in enumerate(angles):
        d = dict(base)
        d['rot'] = a
        d['fl'] = fl[i] if i < len(fl) else 0
        d['armF'] = (lerp(base['armF'][0], 160, i / 5), lerp(base['armF'][1], 120, i / 5))
        d['armB'] = (lerp(base['armB'][0], 140, i / 5), lerp(base['armB'][1], 100, i / 5))
        d['head_dx'] = -i * 0.25
        if extra_each:
            d.update(extra_each(i))
        out.append(d)
    return out

def hurt_poses(base, back=1.8):
    p0 = dict(base); p1 = dict(base)
    p0.update(lean=-back, head_dx=-1.2, head_dy=-0.6, fl=0.75, armF=(110, 130), armB=(70, 100), legF=(10, 8), legB=(-12, -14))
    p1.update(lean=-back * 0.6, head_dx=-0.6, armF=(60, 80), armB=(30, 60), legF=(8, 8), legB=(-9, -9))
    return [p0, p1]


# ------------------------------------------------------------------ PERSONAJES HUMANOIDES
def pal(**kw):
    return {k: hexc(v) for k, v in kw.items()}

ST = dict(thigh=5.4, shin=5.4, hip_y=19.2, torso_h=8.0, neck=3.6, head=(3.5, 3.7), arm1=4.4, arm2=4.2,
          leg_w=2.7, arm_w=2.4, torso_w=6.6, torso_wb=5.6)

def make_ike():
    st = dict(ST, kind='ike', c=pal(body='1f8a9e', trim='ff7a2f', belt='5a3d24', legs='27425e', boot='3a2a20', arm='1f8a9e',
                                      hand='d9a066', skin='d9a066', hair='3a2a20'))
    idle = idle_poses(4)
    walk = walk_poses(6)
    run = walk_poses(6, amp=44, bend=62, arm=44, lean=2.2, base=(10, 55))
    jump = []
    for i in range(3):
        jump.append(dict(legF=[(40, -20), (10, 6), (24, 30)][i], legB=[(-6, 20), (-20, -8), (-24, 8)][i], lean=[0.8, 0.4, -0.4][i],
                         armF=[(150, 120), (110, 90), (70, 60)][i], armB=[(130, 100), (90, 70), (40, 30)][i],
                         dy=[-2.5, -3.5, -2.0][i], grounded=False, head_dy=[-0.3, 0, 0.4][i]))
    for jp in jump:
        jp['grounded'] = True
    shoot = []
    for i in range(3):
        shoot.append(dict(legF=(10, 10), legB=(-10, -10), lean=[0.6, -0.5, 0.2][i], armF=(92, 90), armB=(40, 70), weapon='pistol',
                          flash=[0, 3.4, 0][i], head_dy=0))
    melee = []
    specs = [((215, 200), (30, 60), None), ((160, 140), (30, 60), (290, 350, 7)), ((100, 80), (40, 70), (330, 380, 7)), ((45, 30), (20, 40), None)]
    for i, (af, ab, sw) in enumerate(specs):
        melee.append(dict(legF=(14, 12), legB=(-14, -14), lean=[-0.8, 0.4, 1.6, 1.0][i], armF=af, armB=ab, weapon='wrench',
                          swoosh=sw, hip_x=[0, 0.4, 0.8, 0.4][i]))
    hurt = hurt_poses(dict(idle[0], weapon=None))
    die = die_poses(dict(idle[0]), extra_each=lambda i: {})
    anims = [('Idle', 8, True, idle), ('Walk', 10, True, walk), ('Run', 14, True, run), ('Jump', 8, False, jump),
             ('Shoot', 12, False, shoot), ('Melee', 12, False, melee), ('Hurt', 10, False, hurt), ('Die', 10, False, die)]
    return dict(name='Ike', cell=32, pivot=[0.5, 0.047], st=st, anims=anims)

def make_mara():
    st = dict(ST, kind='mara', c=pal(body='f1f3f5', trim='1f8a9e', belt='5a3d24', legs='2c3a4d', boot='3a2a20', arm='f1f3f5',
                                       hand='c68a5e', skin='c68a5e', hair='1c1410'))
    idle = idle_poses(4, armF=(6, 20), armB=(-3, 8))
    walk = walk_poses(6, amp=24, bend=36, arm=20, lean=0.4)
    talk = []
    for i in range(4):
        ph = 2 * math.pi * i / 4
        d = dict(idle[0]); d.update(armF=(70 + 25 * math.sin(ph), 100 + 30 * math.sin(ph + 1)), head_dy=0.5 * math.sin(ph), lean=0.4 + 0.3 * math.sin(ph))
        talk.append(d)
    anims = [('Idle', 8, True, idle), ('Walk', 10, True, walk), ('Talk', 8, True, talk)]
    return dict(name='Mara', cell=32, pivot=[0.5, 0.047], st=st, anims=anims)

def make_raizal():
    st = dict(ST, kind='raizal', head=(3.4, 3.5), c=pal(body='6b5a3a', legs='4a4a38', boot='3a3a2a', arm='6aa65a', hand='7fbf6c',
                                                         skin='6aa65a', glow='d6ff6a', eye='ffe34d'))
    idle = idle_poses(4, armF=(55, 55), armB=(45, 50), lean=2.0)
    for i, d in enumerate(idle):
        d['mouth'] = 0.4 * (i % 2)
    walk = walk_poses(6, amp=20, bend=34, arm=10, lean=2.4, base=(70, 70))
    run = walk_poses(6, amp=40, bend=60, arm=14, lean=4.0, base=(95, 90))
    attack = []
    specs = [dict(lean=-0.8, armF=(160, 130), armB=(150, 120), mouth=1.0, hip_x=-0.6),
             dict(lean=3.4, armF=(100, 95), armB=(95, 90), mouth=1.6, hip_x=1.6),
             dict(lean=4.0, armF=(85, 80), armB=(80, 75), mouth=1.6, hip_x=2.0),
             dict(lean=1.6, armF=(60, 60), armB=(55, 55), mouth=0.6, hip_x=0.6)]
    for s_ in specs:
        d = dict(legF=(18, 8), legB=(-22, -22), weapon='claws_f'); d.update(s_)
        attack.append(d)
    hurt = hurt_poses(dict(idle[0]))
    die = die_poses(dict(idle[0]))
    anims = [('Idle', 8, True, idle), ('Walk', 8, True, walk), ('Run', 14, True, run), ('Attack', 12, False, attack),
             ('Hurt', 10, False, hurt), ('Die', 10, False, die)]
    return dict(name='Raizal', cell=32, pivot=[0.5, 0.047], st=st, anims=anims)

def make_guardia():
    st = dict(ST, kind='guardia', bulk=1.1, head=(3.5, 3.6), c=pal(body='4a5568', legs='3b4252', boot='20252f', arm='4a5568', hand='20252f',
                                                                    plate='707d92', accent='e03131', head='5b677c', visor='e03131'))
    rifle = dict(weapon='rifle')
    idle = idle_poses(4, armF=(70, 86), armB=(62, 80), extra=rifle)
    walk = walk_poses(6, amp=26, bend=38, arm=6, lean=0.6, base=(70, 86), extra=rifle)
    for d in walk:
        d['armB'] = (62 + 4, 80)
    shoot = []
    for i in range(3):
        shoot.append(dict(legF=(12, 12), legB=(-12, -12), lean=[0.4, -0.7, 0.1][i], armF=(84, 90), armB=(76, 84), weapon='rifle',
                          flash=[0, 4.2, 0][i]))
    hurt = hurt_poses(dict(idle[0]))
    die = die_poses(dict(idle[0]))
    anims = [('Idle', 8, True, idle), ('Walk', 9, True, walk), ('Shoot', 12, False, shoot), ('Hurt', 10, False, hurt), ('Die', 10, False, die)]
    return dict(name='Guardia', cell=32, pivot=[0.5, 0.047], st=st, anims=anims)

ROBOT = dict(kind='robot', thigh=4.6, shin=4.8, hip_y=19.6, torso_h=9.2, neck=3.2, head=(3, 3), arm1=4.6, arm2=4.4,
             leg_w=3.6, arm_w=3.0, torso_w=9.4, torso_wb=7.8)

def make_chatarrero(big=False):
    if big:
        col = pal(body='5a2c2c', legs='3a3f4a', boot='20252f', arm='5a2c2c', hand='7b8696', plate='8a3a3a', accent='ffb703',
                  head='6b3535', eye='ff3b30')
        st = dict(ROBOT, bulk=1.15, c=col)
    else:
        col = pal(body='56657a', legs='3f4a5c', boot='252c38', arm='56657a', hand='7b8696', plate='7d8da4', accent='ff7a2f',
                  head='6b7b92', eye='ff9a3d')
        st = dict(ROBOT, c=col)
    claw = dict(weapon='claw', claw=22)
    idle = idle_poses(4, armF=(8, 20), armB=(-6, 10), lean=0.0, extra=claw)
    for i, d in enumerate(idle):
        d['eye'] = mix(col['eye'], (255, 255, 255), 0.35 * (i % 2))
    walk = walk_poses(6, amp=20, bend=26, arm=12, lean=0.5, base=(10, 20), extra=claw)
    for d in walk:
        d['head_dy'] = 0.4 * abs(d['legF'][0]) / 20
    attack = []
    specs = [dict(armF=(200, 190), lean=-1.2, claw=34), dict(armF=(170, 150), lean=-0.6, claw=38), dict(armF=(95, 60), lean=2.0, claw=8, dust=True, hip_x=1.0),
             dict(armF=(40, 30), lean=1.0, claw=20)]
    if big:
        specs.append(dict(armF=(30, 20), lean=0.4, claw=22))
    for i, s_ in enumerate(specs):
        d = dict(legF=(12, 10), legB=(-12, -14), armB=(-10, 10), weapon='claw'); d.update(s_)
        attack.append(d)
    hurt = hurt_poses(dict(idle[0]), back=1.2)
    for h in hurt:
        h['sparks'] = 3
    def ee(i):
        return dict(sparks=11 + i if i < 5 else 0, eye=hexc('3a1a1a') if i >= 4 else col['eye'],
                    tint=(40, 30, 30, 0.12 * i) if i else None)
    die = die_poses(dict(idle[0]), angles=(0, 8, 24, 50, 76, 90), extra_each=ee)
    anims = [('Idle', 8, True, idle), ('Walk', 8, True, walk), ('Attack', 10, False, attack)]
    if big:
        charge = []
        for i in range(4):
            ph = 2 * math.pi * i / 4
            charge.append(dict(legF=(30 * math.sin(ph), 30 * math.sin(ph) - 30 * max(0, math.cos(ph))), legB=(-30 * math.sin(ph), -30 * math.sin(ph) - 30 * max(0, -math.cos(ph))),
                               lean=5.0, armF=(120, 100), armB=(100, 90), weapon='claw', claw=10, head_dx=1.0, dust=True))
        anims.append(('Charge', 14, True, charge))
    anims += [('Hurt', 10, False, hurt), ('Die', 9, False, die)]
    return dict(name='ChatarreroAlfa' if big else 'Chatarrero', cell=64 if big else 32, pivot=[0.5, 0.047], st=st, anims=anims)

def make_vex():
    st = dict(ST, kind='vex', bulk=1.35, head=(3.4, 3.6), torso_h=8.4, neck=3.4, c=pal(body='343c52', legs='2c3448', boot='151a26', arm='3a4358', hand='151a26',
                                                                                        pad='4a5470', accent='ff3b30', head='3a4560', visor='ff3b30', crystal='35e0ff'))
    cn = dict(weapon='cannon')
    idle = idle_poses(4, armF=(72, 88), armB=(-6, 10), lean=0.3, extra=cn)
    walk = walk_poses(6, amp=24, bend=32, arm=6, lean=0.6, base=(72, 88), extra=cn)
    for d in walk:
        d['armB'] = (-d['armB'][0] * 0.4, 10)
    shoot = []
    for i in range(4):
        shoot.append(dict(legF=(14, 12), legB=(-14, -14), lean=[0.4, -0.6, -0.3, 0.2][i], armF=(90, 90), armB=(-10, 20), weapon='cannon',
                          flash=[0, 5.0, 3.0, 0][i]))
    slam = []
    specs = [dict(armF=(190, 170), lean=-0.8), dict(armF=(175, 150), lean=-1.4), dict(armF=(95, 70), lean=2.2, dust=True, hip_x=1.0, flash=3.6),
             dict(armF=(75, 60), lean=1.8, dust=True), dict(armF=(60, 70), lean=0.6)]
    for s_ in specs:
        d = dict(legF=(16, 12), legB=(-16, -16), armB=(-12, 20), weapon='cannon'); d.update(s_)
        slam.append(d)
    hurt = hurt_poses(dict(idle[0]), back=1.2)
    for h in hurt:
        h['weapon'] = 'cannon'
    die = die_poses(dict(idle[0]), angles=(0, 10, 30, 54, 78, 90), extra_each=lambda i: dict(sparks=21 + i if i < 5 else 0, tint=(30, 10, 10, 0.1 * i) if i else None))
    anims = [('Idle', 8, True, idle), ('Walk', 9, True, walk), ('Shoot', 12, False, shoot), ('Slam', 10, False, slam), ('Hurt', 10, False, hurt), ('Die', 9, False, die)]
    return dict(name='Vex', cell=48, pivot=[0.5, 0.047], st=st, anims=anims)


# ------------------------------------------------------------------ CRIATURAS ESPECIALES (dibujo propio)
def f_drone(t, mode):
    cv = Cv(32)
    cx = 15.5
    bob = 1.0 * math.sin(2 * math.pi * t)
    cy = 15 + bob
    tilt = 0
    flash = 0
    rot = 0
    dy = 0
    dx = 0
    flashw = 0
    if mode == 'move':
        tilt = 3
    body = hexc('5b6e8c'); dome = hexc('8aa0c4'); eye = hexc('35e0ff')
    if mode == 'hurt':
        eye = hexc('ff3b30')
    # sombra de propulsor
    fl = 1.5 + 1.2 * (0.5 + 0.5 * math.sin(2 * math.pi * t * 2))
    cv.poly([(cx - 3, cy + 3.6), (cx + 3, cy + 3.6), (cx + 0.5, cy + 3.6 + fl * 2.2), (cx - 0.5, cy + 3.6 + fl * 2.2)], hexc('ff922b'))
    cv.poly([(cx - 1.6, cy + 3.6), (cx + 1.6, cy + 3.6), (cx, cy + 3.6 + fl * 1.3)], hexc('ffe066'))
    # cuerpo
    cv.ell(cx, cy, 9, 4.6, body)
    cv.ell(cx - 0.5, cy - 2.4, 6.2, 3.6, dome)
    cv.rect(cx - 8, cy + 0.2, cx + 8, cy + 1.2, sh(body, 0.7))
    cv.rect(cx - 3.5, cy + 3.8, cx + 3.5, cy + 4.8, hexc('3a4660'))
    # ojo
    cv.ell(cx + 4.8, cy - 0.4, 2.7, 2.6, hexc('1b2233'))
    cv.ell(cx + 5.1, cy - 0.4, 1.7, 1.7, eye)
    cv.ell(cx + 5.5, cy - 0.9, 0.6, 0.6, hexc('ffffff'))
    # rotor
    ph = (t * 4) % 1
    ln = 9 * (0.55 + 0.45 * abs(math.sin(math.pi * ph * 2)))
    cv.rect(cx - ln, cy - 6.6, cx + ln, cy - 5.8, hexc('c9d1db'))
    cv.rect(cx - 0.6, cy - 6.6, cx + 0.6, cy - 5.0, hexc('3a4660'))
    cv.ell(cx - 8.6, cy + 0.8, 0.9, 0.9, hexc('ff7a2f'))
    return cv

def drone_frames():
    frames = {}
    idle = []
    for i in range(4):
        idle.append(f_drone(i / 4, 'idle').finish())
    move = []
    for i in range(4):
        cv = f_drone(i / 4, 'move')
        move.append(cv.finish(rot=-8, pivot=(16, 16)))
    shoot = []
    for i in range(3):
        cv = f_drone(0.1, 'idle')
        if i == 0:
            cv.ell(21, 14.6, 4.2, 4.2, hexc('35e0ff'))
            cv.ell(21, 14.6, 2.0, 2.0, hexc('ffffff'))
        if i == 1:
            cv.rect(20.5, 14.0, 32, 15.4, hexc('35e0ff'))
            cv.rect(20.5, 14.4, 32, 15.0, hexc('ffffff'))
            cv.ell(21, 14.6, 4.0, 4.0, hexc('ffffff'))
        if i == 2:
            cv.ell(21, 14.6, 2.6, 2.6, hexc('35e0ff'))
        shoot.append(cv.finish(dx=-1.2 if i == 1 else 0, pivot=(16, 16)))
    hurt = []
    for i in range(2):
        cv = f_drone(0.2, 'hurt')
        cv.sparks(5 + i, 8, 16, 14, 9, [hexc('ffd43b'), hexc('ffffff'), hexc('ff922b')])
        hurt.append(cv.finish(flash=0.7 if i == 0 else 0, dx=-1.5 if i == 0 else -0.6, pivot=(16, 16)))
    die = []
    for i in range(5):
        cv = f_drone(0.3 + i * 0.1, 'hurt')
        if i < 3:
            cv.sparks(30 + i, 9, 16, 14, 10, [hexc('ffd43b'), hexc('ff922b')])
            cv.ell(14, 8 - i, 2.2, 1.6, hexc('3a4660'))
            die.append(cv.finish(rot=25 * (i + 1), pivot=(16, 16), dy=3.5 * i, dx=-i * 1.2, tint=(40, 30, 30, 0.15 * i)))
        elif i == 3:
            cv2 = Cv(32)
            cv2.ell(16, 24, 11, 8, hexc('ff7a2f'))
            cv2.ell(16, 24, 8, 6, hexc('ffd43b'))
            cv2.ell(16, 24, 4, 3, hexc('ffffff'))
            die.append(cv2.finish())
        else:
            cv2 = Cv(32)
            cv2.ell(16, 25, 12, 5, hexc('4a4a52'))
            cv2.ell(16, 26, 8, 3, hexc('2a2a32'))
            cv2.sparks(77, 12, 16, 22, 11, [hexc('ff922b'), hexc('adb5bd')])
            die.append(cv2.finish())
    return [('Idle', 8, True, idle), ('Move', 10, True, move), ('Shoot', 10, False, shoot), ('Hurt', 10, False, hurt), ('Die', 8, False, die)]


def f_spore(r, glow, spikes=8, tilt=0, trail=False, wob=0):
    cv = Cv(32)
    cx, cy = 16, 15.5
    skin = hexc('5da05a'); dark = hexc('2f6b3a')
    for i in range(spikes):
        a = 2 * math.pi * i / spikes + 0.3
        bx, by = cx + (r - 0.6) * math.cos(a), cy + (r - 0.6) * math.sin(a)
        tx, ty = cx + (r + 3.0) * math.cos(a), cy + (r + 3.0) * math.sin(a)
        pa = a + math.pi / 2
        cv.poly([(bx + math.cos(pa) * 1.3, by + math.sin(pa) * 1.3), (tx, ty), (bx - math.cos(pa) * 1.3, by - math.sin(pa) * 1.3)], dark)
    for k in range(3):
        x0 = cx - 3 + 3 * k
        cv.line([(x0, cy + r - 1), (x0 + 1.2 * math.sin(wob + k), cy + r + 3), (x0 - 0.8 * math.sin(wob + k * 2), cy + r + 6)], 0.9, dark)
    cv.ell(cx, cy, r, r, skin)
    cv.ell(cx - 1.4, cy - 1.8, r * 0.55, r * 0.45, hexc('86c97f'))
    cv.ell(cx + 1.0, cy + 0.5, glow, glow, hexc('d6ff6a'))
    cv.ell(cx + 1.0, cy + 0.5, glow * 0.5, glow * 0.5, hexc('ffffff'))
    return cv

def spore_frames():
    idle = []
    for i in range(4):
        ph = 2 * math.pi * i / 4
        idle.append(f_spore(6.2 + 0.5 * math.sin(ph), 2.8 + 0.5 * math.sin(ph), wob=ph).finish())
    move = []
    for i in range(4):
        ph = 2 * math.pi * i / 4
        cv = f_spore(6.0 - 0.3 * math.sin(ph), 3.0, tilt=0, wob=ph * 2)
        cv.rect(2 + 0.6 * i, 14, 6 + 0.6 * i, 14.7, hexc('86c97f'))
        move.append(cv.finish(rot=-10, pivot=(16, 16), dx=0.8 * math.sin(ph)))
    boom = []
    for i in range(6):
        cv = Cv(32)
        if i == 0:
            cv = f_spore(8, 5.0)
            boom.append(cv.finish(flash=0.3))
        elif i == 1:
            cv.ell(16, 16, 10, 10, hexc('d6ff6a'))
            cv.ell(16, 16, 6, 6, hexc('ffffff'))
            boom.append(cv.finish())
        elif i == 2:
            cv.ell(16, 16, 14, 14, hexc('9be15d'))
            cv.ell(16, 16, 10, 10, hexc('d6ff6a'))
            cv.ell(16, 16, 4, 4, hexc('ffffff'))
            boom.append(cv.finish())
        elif i == 3:
            cv.ell(16, 16, 15, 15, hexc('5da05a'))
            cv.ell(16, 16, 11, 11, hexc('9be15d'))
            cv.sparks(i, 16, 16, 16, 13, [hexc('d6ff6a'), hexc('ffffff')])
            boom.append(cv.finish())
        elif i == 4:
            cv.sparks(i, 22, 16, 16, 14, [hexc('d6ff6a'), hexc('5da05a'), hexc('2f6b3a')])
            cv.ell(16, 16, 8, 8, hexc('3f7d44'))
            boom.append(cv.finish())
        else:
            cv.sparks(i, 12, 16, 17, 13, [hexc('5da05a'), hexc('2f6b3a')])
            boom.append(cv.finish())
    return [('Idle', 8, True, idle), ('Move', 10, True, move), ('Explode', 12, False, boom)]


# ---- Guardian del reactor (64x64)
def f_guardian(open_=0.0, pulse=0.0, arms=(0, 0), flash=0, fire=0, die=0, hurt=False):
    cv = Cv(64)
    steel = hexc('4a5568'); dark = hexc('2f3747'); light = hexc('707d92')
    if die >= 4:
        steel = hexc('3a3f4a'); dark = hexc('23262e'); light = hexc('4a4f5a')
    # pilares / piernas
    cv.rect(9.5, 22, 14.5, 30.4, dark); cv.rect(17.5, 22, 22.5, 30.4, dark)
    cv.rect(8.5, 28.4, 15.5, 30.4, steel); cv.rect(16.5, 28.4, 23.5, 30.4, steel)
    # torso
    cv.rect(8.5, 8.5, 23.5, 22.5, steel)
    cv.rect(8.5, 8.5, 23.5, 10.5, light)
    cv.rect(8.5, 20.5, 23.5, 22.5, dark)
    # hombros
    cv.rect(5.5, 8, 9.5, 12.5, light); cv.rect(22.5, 8, 26.5, 12.5, light)
    # cabeza
    cv.rect(12.5, 3.2, 19.5, 8.5, light)
    ec = hexc('ff3b30') if die < 4 else hexc('3a1a1a')
    cv.rect(14.0, 5.0, 19.0, 6.6, ec)
    cv.rect(17.0, 5.3, 18.6, 6.1, mix(ec, (255, 255, 255), 0.6))
    cv.rect(15.7, 1.6, 16.5, 3.4, dark)
    # pecho: núcleo
    core_c = mix(hexc('ff3b30'), (255, 255, 255), 0.25 * pulse)
    cv.rect(11.5, 11.0, 20.5, 19.5, dark)
    if open_ > 0.05 or die:
        cv.ell(16, 15.2, 3.4 + 0.5 * pulse, 3.4 + 0.5 * pulse, core_c if die < 4 else hexc('4a2a2a'))
        if die < 4:
            cv.ell(16, 15.2, 1.6, 1.6, hexc('ffffff'))
    # placas del pecho
    off = 4.6 * open_
    cv.rect(11.5 - off * 0.2, 11.0, 16.0 - off, 19.5, light)
    cv.rect(16.0 + off, 11.0, 20.5 + off * 0.2, 19.5, light)
    cv.rect(11.5, 14.6, 20.5, 15.1, dark) if open_ < 0.2 else None
    # brazos
    for side, a in ((-1, arms[0]), (1, arms[1])):
        sx0 = 7.5 if side < 0 else 24.5
        ex, ey = cv.limb(sx0, 11, 6, a * side * -1 if False else (-a if side < 0 else a), 3.4, dark)
        ex2, ey2 = cv.limb(ex, ey, 6, (-a if side < 0 else a) * 0.7 + 0, 3.2, steel)
        cv.ell(ex2, ey2, 2.2, 2.2, light)
    if fire:
        cv.star(27.5, 12, 4.5 + fire, hexc('35e0ff'), hexc('ffffff'))
        cv.rect(28, 11.4, 31.8, 12.6, hexc('35e0ff'))
    return cv

def guardian_frames():
    idle = [f_guardian(0, math.sin(2 * math.pi * i / 4) * 0.5 + 0.5).finish() for i in range(4)]
    opn = [f_guardian(v, 1).finish() for v in (0.25, 0.6, 0.9, 1.0)]
    fire = []
    for i in range(4):
        cv = f_guardian(1, 1, arms=(10, [95, 100, 95, 60][i]), fire=[0, 1.6, 0.6, 0][i])
        fire.append(cv.finish())
    hurt = [f_guardian(1, 1).finish(flash=0.7), f_guardian(1, 0.5).finish(dx=0.6)]
    die = []
    for i in range(6):
        cv = f_guardian(1 if i < 4 else 0, 0.5, die=i)
        cv.sparks(40 + i, 14 if i < 5 else 0, 16, 14, 11, [hexc('ffd43b'), hexc('ff922b'), hexc('ffffff')])
        die.append(cv.finish(flash=0.6 if i == 0 else 0, rot=[0, 2, 4, 6, 8, 10][i] * -1, pivot=(16, 30), dx=0, squash=[1, 1, 0.97, 0.92, 0.85, 0.78][i],
                             tint=(30, 20, 20, 0.12 * i) if i else None))
    return [('Idle', 6, True, idle), ('Open', 8, False, opn), ('Fire', 10, False, fire), ('Hurt', 10, False, hurt), ('Die', 8, False, die)]


# ---- Madre Raiz (96x96)
def f_madre(t, strike=0.0, bulbs=1.0, hurt=False, die=0, warn=False):
    cv = Cv(96)
    leaf = hexc('2f6b3a'); dark = hexc('1b3a24'); mid = hexc('3f8a4a'); glow = hexc('d6ff6a')
    if die:
        leaf = mix(leaf, hexc('6b5a3a'), min(1, die / 5)); mid = mix(mid, hexc('7a6a45'), min(1, die / 5)); dark = mix(dark, hexc('3a2f20'), min(1, die / 5))
    ph = 2 * math.pi * t
    # raíces traseras
    for k, (side, base) in enumerate([(-1, 5), (-1, 9), (1, 4), (1, 9), (-1, 12), (1, 13)]):
        pts = []
        L = 15 + 2 * (k % 3)
        for u in range(0, 11):
            uu = u / 10
            x = 16 + side * (base + uu * L)
            y = 29.5 - 5 * math.sin(math.pi * uu * 0.9) * (1 - 0.1 * k) + 1.2 * math.sin(ph + k + uu * 4) * (0 if die > 3 else 1)
            pts.append((x, y))
        cv.line(pts, 2.4 - 0.2 * (k % 3), dark if k % 2 else leaf)
    # montículo y tronco
    cv.ell(16, 29.8, 13.5, 3.4, dark)
    cv.ell(16, 19, 7.4, 12, leaf)
    cv.ell(14, 17, 4.2, 9, mid)
    for i in range(4):
        cv.line([(11 + i * 3, 29), (11 + i * 3 + 0.8, 22), (12 + i * 3, 14)], 0.5, dark)
    # cara / ojos
    ec = glow if die < 4 else hexc('3a2f20')
    cv.ell(14.0, 15.5, 1.3, 1.0, ec); cv.ell(19.0, 15.5, 1.3, 1.0, ec)
    cv.rect(13.5, 19.5, 19.5, 20.4, dark)
    # bulbos
    for j, (bx, by) in enumerate([(9.5, 10.5), (22.5, 10.5), (16, 5.5)]):
        r = (2.7 + 0.4 * math.sin(ph + j)) * bulbs
        if die >= 3:
            r = 1.6
        cv.line([(16, 12), (bx, by + 2)], 1.0, dark)
        cv.ell(bx, by, r + 0.6, r + 0.6, hexc('c2f06a') if die < 3 else hexc('6b5a3a'))
        cv.ell(bx, by, r, r, hexc('e9ff8a') if die < 3 else hexc('7a6a45'))
        if die < 3:
            cv.ell(bx, by, r * 0.45, r * 0.45, hexc('ffffff'))
    # raíces delanteras
    for k, (side, base) in enumerate([(-1, 7), (1, 6), (1, 11)]):
        pts = []
        for u in range(0, 11):
            uu = u / 10
            x = 16 + side * (base + uu * 11)
            y = 30 - 4 * math.sin(math.pi * uu) + 0.9 * math.sin(ph * 2 + k + uu * 5) * (0 if die > 3 else 1)
            pts.append((x, y))
        cv.line(pts, 2.2, mid)
    # ataque: gran raíz que golpea hacia la derecha
    if strike > 0:
        up = min(1, strike * 1.6)
        down = max(0, strike * 1.6 - 1)
        pts = []
        tx, ty = 28.5, lerp(3, 29, down)
        for u in range(0, 13):
            uu = u / 12
            x = lerp(18, tx, uu) + 3 * math.sin(math.pi * uu) * (1 - down * 0.8)
            y = lerp(27, ty, uu) - 20 * math.sin(math.pi * uu) * up * (1 - down)
            pts.append((x, y))
        cv.line(pts, 3.2, leaf)
        cv.line(pts[-5:], 2.0, mid)
        cv.ell(pts[-1][0], pts[-1][1], 1.8, 1.8, dark)
        if down > 0.7:
            for i in range(4):
                cv.ell(24 + i * 2.4, 29.4 - (i % 2), 1.3, 1.0, hexc('c9d1db'))
    if warn:
        cv.ell(28.5, 29.7, 3.8, 0.9, hexc('ff3b30'))
    # esporas flotantes
    if not die or die < 4:
        for q in range(7):
            sx_ = 16 + 12 * math.sin(ph + q * 0.9) * (0.5 + q / 10)
            sy_ = 24 - ((t * 20 + q * 5) % 22)
            cv.rect(sx_, sy_, sx_ + 0.9, sy_ + 0.9, hexc('d6ff6a'))
    return cv

def madre_frames():
    idle = [f_madre(i / 6).finish() for i in range(6)]
    atk = []
    for i, (s_, w_) in enumerate([(0.0, True), (0.25, True), (0.5, False), (0.75, False), (1.0, False), (1.0, False)]):
        sv = [0.12, 0.3, 0.55, 0.8, 1.0, 0.9][i]
        atk.append(f_madre(i / 6, strike=sv, warn=(i < 2)).finish())
    hurt = [f_madre(0.1, hurt=True).finish(flash=0.7), f_madre(0.2).finish(dx=-0.6)]
    die = []
    for i in range(6):
        cv = f_madre(0.3 + i * 0.05, die=i, bulbs=1 - i * 0.12)
        die.append(cv.finish(flash=0.6 if i == 0 else 0, squash=[1, 1, 0.96, 0.9, 0.82, 0.74][i], tint=(30, 25, 10, 0.1 * i) if i else None))
    return [('Idle', 6, True, idle), ('Attack', 10, False, atk), ('Hurt', 10, False, hurt), ('Die', 8, False, die)]


# ------------------------------------------------------------------ ENSAMBLADO
def humanoid_frames(ch):
    out = []
    for (name, fps, loop, poses) in ch['anims']:
        frames = [render_h(ch['cell'], ch['st'], p) for p in poses]
        out.append((name, fps, loop, frames))
    return out

def build_all():
    chars = []
    for mk in (make_ike, make_mara, make_raizal, make_guardia, make_chatarrero, lambda: make_chatarrero(True), make_vex):
        ch = mk()
        chars.append(dict(name=ch['name'], cell=ch['cell'], pivot=ch['pivot'], anims=humanoid_frames(ch)))
    chars.append(dict(name='Dron', cell=32, pivot=[0.5, 0.5], anims=drone_frames()))
    chars.append(dict(name='Espora', cell=32, pivot=[0.5, 0.5], anims=spore_frames()))
    chars.append(dict(name='GuardianReactor', cell=64, pivot=[0.5, 0.047], anims=guardian_frames()))
    chars.append(dict(name='MadreRaiz', cell=96, pivot=[0.5, 0.047], anims=madre_frames()))
    return chars

DISPLAY = {'Ike': 'Ike Ramos (jugador)', 'Mara': 'Dra. Mara Quiroz', 'Raizal': 'Raizal', 'Guardia': 'Guardia Corrupto',
           'Chatarrero': 'Chatarrero', 'ChatarreroAlfa': 'Chatarrero Alfa (jefe)', 'Vex': 'Cmdt. Vex / Titan-V (jefe final)',
           'Dron': 'Dron Centinela', 'Espora': 'Espora Explosiva', 'GuardianReactor': 'Guardian del Reactor (jefe)', 'MadreRaiz': 'Madre Raiz (jefe)'}

if __name__ == '__main__':
    out = '/mnt/user-data/outputs/Poltor7_Sprites2D'
    for sub in ('sheets', 'strips', 'frames', 'preview'):
        os.makedirs(os.path.join(out, sub), exist_ok=True)
    chars = build_all()
    meta = {'ppu': 32, 'characters': []}
    font = ImageFont.load_default()
    for ch in chars:
        cell = ch['cell']
        maxf = max(len(a[3]) for a in ch['anims'])
        sheet = Image.new('RGBA', (maxf * cell, len(ch['anims']) * cell), (0, 0, 0, 0))
        entry = dict(name=ch['name'], display=DISPLAY[ch['name']], file='%s_sheet.png' % ch['name'], cell=cell, pivotX=ch['pivot'][0], pivotY=ch['pivot'][1], anims=[])
        for r, (name, fps, loop, frames) in enumerate(ch['anims']):
            strip = Image.new('RGBA', (len(frames) * cell, cell), (0, 0, 0, 0))
            for c, fr in enumerate(frames):
                sheet.paste(fr, (c * cell, r * cell))
                strip.paste(fr, (c * cell, 0))
                os.makedirs(os.path.join(out, 'frames', ch['name']), exist_ok=True)
                fr.save(os.path.join(out, 'frames', ch['name'], '%s_%s_%02d.png' % (ch['name'], name, c)))
            strip.save(os.path.join(out, 'strips', '%s_%s_strip.png' % (ch['name'], name)))
            entry['anims'].append(dict(name=name, row=r, frames=len(frames), fps=fps, loop=loop))
        sheet.save(os.path.join(out, 'sheets', '%s_sheet.png' % ch['name']))
        meta['characters'].append(entry)
        # GIF de vista previa
        sc = 6 if cell <= 48 else 4
        gframes, durs = [], []
        for (name, fps, loop, frames) in ch['anims']:
            for fr in frames:
                bg = Image.new('RGBA', (cell * sc, cell * sc + 18), (22, 28, 44, 255))
                big = fr.resize((cell * sc, cell * sc), Image.NEAREST)
                bg.alpha_composite(big, (0, 18))
                d = ImageDraw.Draw(bg)
                d.text((4, 3), '%s - %s' % (ch['name'], name), fill=(127, 219, 240, 255), font=font)
                gframes.append(bg.convert('P', palette=Image.ADAPTIVE))
                durs.append(int(1000 / fps))
        gframes[0].save(os.path.join(out, 'preview', '%s_preview.gif' % ch['name']), save_all=True, append_images=gframes[1:], duration=durs, loop=0, disposal=2)
    json.dump(meta, open(os.path.join(out, 'animations.json'), 'w', encoding='utf8'), indent=2, ensure_ascii=False)
    print('listo', [c['name'] for c in chars])
