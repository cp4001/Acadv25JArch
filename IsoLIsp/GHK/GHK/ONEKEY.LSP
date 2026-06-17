;**********************ONEKEY.LSP******************************
;   One-Key Command Program  Ver. 1.1
;      By   ENVIRONMENTAL PROFESSIONALS GROUP, KOREA
;           TEL; 567-6101   1991.9.24
;      All are loaded by : (load "ONEKEY")
;      And run by
;                 Command:<each one-key command>
;***************************************************************

;Line Joint LISP (c) 1991,10 Kim Deok Keun

(defun lineerr(err)
  (prompt "\n*error*:")
  (prin1 err)
  (command "redraw")
  (setq *error* olderr)
);** defun end **

(defun C:JOINT (/ ob1 ob2 ob3 ent1 ent2 ent3 s1 e1 l1 s2 e2 l2 p1 p2 p3 p4
                     p5 ang1 ang2 loop)
  (setvar "cmdecho" 0)
  (setq olderr *error*
        *error* lineerr
        loop T
  );** setq end **
  (while loop
    (setq ob1 (entsel "\n>>>Select 1st Line: "))
    (if (= ob1 nil) (setq loop T) (setq loop nil))
  );** while end **
  (setq ent1 (entget (car ob1))
        loop T
  );** setq end **
  (redraw (car ob1) 3)
  (while loop
    (setq ob2 (entsel "\n>>>Select 2nd Line: "))
    (if (= ob2 nil) (setq loop T) (setq loop nil))
  );** while end **
  (setq ent2 (entget (car ob2))
        s1 (cdr (assoc 10 ent1))
        e1 (cdr (assoc 11 ent1))
        l1 (cdr (assoc 8 ent1))
        s2 (cdr (assoc 10 ent2))
        e2 (cdr (assoc 11 ent2))
        l2 (cdr (assoc 8 ent2))
        p1 (distance s1 s2)
        p2 (distance e1 s2)
        p3 (distance s1 e2)
        p4 (distance e1 e2)
        la 1
        lin 1
  );** setq end **
  (if (> p1 p2)
    (progn
      (setq p5 p1
            sp (list '10 (car s1) (cadr s1))
            ep (list '11 (car s2) (cadr s2))
      );** setq end **
    );** progn end **
    (progn
       (setq p5 p2
             sp (list '10 (car e1) (cadr e1))
             ep (list '11 (car s2) (cadr s2))
       );** setq end **
    );** progn end **
  );** if end **
  (if (< p5 p3)
    (progn
      (setq p5 p3
            sp (list '10 (car s1) (cadr s1))
            ep (list '11 (car e2) (cadr e2))
      );** setq end **
    );** progn end **
  );** if end **
  (if (< p5 p4)
     (progn
       (setq p5 p4
             sp (list '10 (car e1) (cadr e1))
             ep (list '11 (car e2) (cadr e2))
       );** setq end **
     );** progn end **
  );** if end **
  (setq ang1 (atof (angtos (angle (cdr sp) (cdr ep)) 0 3))
        ang2 (atof (angtos (angle s1 e1) 0 3))
  );** setq end **
  (if (>= ang1 270) (setq ang1 (- ang1 180)))
  (if (>= ang1 180) (setq ang1 (- ang1 180)))
  (if (>= ang2 270) (setq ang2 (- ang2 180)))
  (if (>= ang2 180) (setq ang2 (- ang2 180)))
  (if (= ang1 ang2)
    (progn
      (if (/= l1 l2)
        (progn
          (redraw (car ob2) 3)
          (setq ob3 (entsel "\n>>>Select object to layer: ")
                ent3 (entget (car ob3))
                ent1 (subst (assoc 8 ent3) (assoc 8 ent1) ent1)
          );** setq end **
        );** progn end **
      );** if end **
      (command "erase" ob2 "")
      (setq ent1 (subst sp (assoc 10 ent1) ent1)
            ent1 (subst ep (assoc 11 ent1) ent1)
      );** setq end **
      (entmod ent1)
    );** progn end **
    (progn
      (redraw (car ob1) 4)
      (redraw (car ob2) 4)
      (princ "\nCan't joint.....")
    );** else progn end **
  );** if end **
  (setq *error* olderr)
  (prin1)
);** end of lisp **




(defun c:INSUL (/ cc ck sa p1 p2 ang l)
  (setvar "cmdecho" 0)
  (setq sa (getvar "snapang"))
  (if (/= c nill)
    (setq cc (strcat "\nEnter Insulation Thickness<" (rtos c 2) ">(mm): ")
          ck (getreal cc)
    )
    (setq c 50.0
          cc (strcat "\nEnter Insulation Thickness<" (rtos c 2) ">(mm): ")
          ck (getreal cc)
    )
  )
  (if (numberp ck) (setq c ck))
  (setq p1 (getpoint
     "\nPick First Point (Bottom-right corner): "))
  (setq p2 (getpoint
     p1 "\nPick Second Point (Top-right corner): "))
  (setq ang (angle p1 p2))
  (setq l (distance p1 p2))
  (command "insert" "*insul" p1 c (/ (* 180 ang) pi))
  (setvar "snapang" ang)
  (command "array" "l" "" "r" "1" (fix (/ l (* 0.4 c))) (* 0.4 c))
  (setvar "snapang" sa)
  (setvar "cmdecho" 1)
)
(DEFUN C:MT1()
(SETVAR "HIGHLIGHT" 1)
(princ "\nTrim entities crossing a line ... ")
 (initget 1)
 (setq pt1(getpoint "\nFirst point: "))
 (initget 1)
 (setq pt2(getpoint pt1 "\nSecond point: "))
 (setq lgroup(ssget "c" pt1 pt2))
 (command "line" pt1 pt2 "")
 (initget 1)
 (setq pt3(getpoint "\nTrim which side: "))
 (if lgroup(progn(setq count 0)
    (while(< count(sslength lgroup))
     (setq temp(ssname lgroup count)tlist(entget temp))
      (if(=(cdr(assoc 0 tlist))"LINE")
      (progn(setq tpt1(cdr(assoc 10 tlist))tpt2(cdr(assoc 11 tlist)))
      (setq intpt(inters(list(car pt1)(cadr pt1))(list(car pt2)(cadr pt2))
      (list(car tpt1)(cadr tpt1))(list(car tpt2)(cadr tpt2))))
 (if intpt(progn(setq inpt(list(car intpt)(cadr intpt)(caddr pt1)))
 (setq tpt1(polar intpt(angle intpt tpt1)(/(distance intpt tpt1)2.)))
 (setq tpt2(polar intpt(angle intpt tpt2)(/(distance intpt tpt2)2.)))
 (if(inters pt1 pt2 tpt1 pt3)
 (command "trim" "l" "" tpt2 "")
 (command "trim" "l" "" tpt1 ""))))))(setq count(1+ count)))))
 (command "erase" "l" "")
 (setq lgroup nil pt1 nil pt2 nil pt3 nil count nil tlist nil)
 (setq intpt nil tpt1 nil tpt2 nil)(princ))
(DEFUN C:ME1()
  (SETVAR "HIGHLIGHT" 1)
  (PROMPT "\n SELECT EXTEND LINE :")
  (SETQ M_E1 (ENTSEL))
  (SETQ M_PT1 (GETPOINT "\n SELECT OBJECT LINE <FIRST POINT> : "))
  (SETQ M_PT2 (GETPOINT M_PT1 "\n SELECT OBJECT LINE <SECOND POINT> :"))
  (SETQ M_ENT1 (SSGET "C" M_PT1 M_PT2) NO 0)
  (SETQ M_SU (SSLENGTH M_ENT1))
  (COMMAND "EXTEND" M_E1 "")
  (WHILE (< NO M_SU)
    (COMMAND (LIST (SSNAME M_ENT1 NO) M_PT2))
    (SETQ NO (+ NO 1))
    )
  (COMMAND PAUSE)
  )
(defun C:Ar()
       (setvar "cmdecho" 0)
       (princ " = ARRAY")
       (command "array")(prin1))

(defun C:ARr  ()
       (princ " = ARRAY - Rectnagular")
       (setq ss (ssget))
       (command "ARRAY" "p" "" "R")(prin1))

(defun C:AL  ()
       (princ " = ARRAY - Last object- Rectangular")
       (command "ARRAY" "L" "" "R")(prin1))

(defun C:BL()
       (setvar "cmdecho" 0)
       (princ " = BLOCK")
       (COMMAND "BLOCK") (PRIN1))

(defun C:B(/ ss sno pt1 pt2 no x)
       (setvar "cmdecho" 0)
       (princ " = BREAK2P")
       (setq ss (ssget))
       (setq sno (sslength ss))
       (setq pt1 (getpoint "\nEnter 1st Point : "))
       (setq pt2 (getpoint pt1 "\nEnter 2nd Point : "))
       (setq no 0)
       (repeat sno
           (setq x (ssname ss no))
           (command "break" x pt1 pt2)
           (setq no (1+ no))
       )(prin1))

(defun c:b1()
       (setvar "cmdecho" 0)
       (princ " = BREAK1P")
       (setq ss (ssget))
       (setq sno (sslength ss))
       (setq pt1 (getpoint "\nEnter 1st Point : "))
       (setq no 0)
       (repeat sno
           (setq x (ssname ss no))
           (command "break" x pt1 "@")
           (setq no (1+ no))
       )(prin1))

(defun C:BR(/ ss sno pt1 pt2 no x)
       (setvar "cmdecho" 0)
       (princ " = BREAK2P")
       (setq ss (ssget))
       (setq sno (sslength ss))
       (setq pt1 (getpoint "\nEnter 1st Point : "))
       (setq pt2 (getpoint pt1 "\nEnter 2nd Point : "))
       (setq no 0)
       (repeat sno
           (setq x (ssname ss no))
           (command "break" x pt1 pt2)
           (setq no (1+ no))
       )(prin1))

(defun C:C()
       (princ " = COPY")
       (setvar "cmdecho" 0)
       (command "copy")(prin1))

(defun C:CM  ()
       (princ " = COPY - multiple") 
       (command "copy" "m")(prin1))
 
(defun C:CW  ()
       (princ " = COPY - Window")
       (command "copy" "w")(prin1))

(defun C:CCC ()
       (princ " = COPY - Crossing")
       (command "copy" "crossing")(prin1))

(defun C:CMD ()
       (princ " = CMDECHO-0")
       (setvar "cmdecho" 0)(prin1))

(defun C:CH()
       (setvar "cmdecho" 0)
       (princ " = CHANGE")
       (command "change")(prin1))

(defun C:CHW ()
       (princ " = CHANGE - WINDOW")
       (command "change""W")(prin1))

(defun C:CHC ()
       (princ " = CHANGE - CROSSING")
       (command "change""C")(prin1))

(defun C:CL ()
      (princ " = CHANGE - Layer")
      (setq ss (ssget))
      (command "change" "p" "" "la")(prin1))

(defun C:CF ()
      (princ " = CHAMFER")
      (command "CHAMFER")(prin1))

(defun C:CI()
       (setvar "cmdecho" 0)
       (princ " = CIRCLE")
       (command "circle")(prin1))

(defun C:CT (/ A B C D E)
  (prompt "\nSelect text to edit: ")
  (setq A (ssget))
  (setq B (sslength A))
  (setq C 0)
  (while (<= 1 B)
    (setq D (ssname A C))
    (if (eq (cdr (assoc 0
      (entget D))) "TEXT")
    (progn
    (redraw D 3)
    (prompt "\nNew text string <")
    (princ (cdr (assoc 1 (entget d))))
    (princ ">:")
    (setq E (getstring T ))
    (if (= E "")(setq E (cdr (assoc 1
      (entget D)))))
    (entmod (subst (cons 1 E)(assoc 1
      (entget D)) (entget D)))
    (redraw D 4)))
    (setq B (- B 1))
    (setq C (+ C 1))
  )
  (princ )
)


(defun C:CHH (/ p m s n hg e t2)
  (setq p (ssget))
  (if p 
    (progn
    (setq m 0 s 0 n (sslength p))
    (setq hg (getdist
      "\nEnter new text height: "))
    (while (< m n)
      (if (= "TEXT" (cdr (assoc 0 (setq
          e (entget (ssname p m))))))
        (progn
        (setq t2 (assoc 40 e))
        (setq e (subst (cons 40 hg) t2
          e))
        (entmod e)
        (setq s (1+ s))
        (setq m (1+ m))
        )
      (setq m (1+ m))
      )
    )
    ))
  (prin1 s)
  (princ " text entities changed.")
  (terpri)
  (setq p nil)
)

(defun C:D()
       (setvar "cmdecho" 0)
       (princ " = DISTANCE")
       (command "dist")(prin1))

(defun C:DT()
       (setvar "cmdecho" 0)
       (princ " = DTEXT-CHANGE LAYER TEXT")
       (command "layer" "m" "text" "c" "2" "" "")
       (command "DTEXT")(prin1))

(defun c:din()
  (setvar "orthomode" 0)
  (setvar "osmode" 4)
  (setq p1(getpoint "첫번째 전등을 선택:")
        p2(getpoint p1 "두번째전등을 선택:"))
       (command "line" p1 p2 "")
  (setq ob1 (entlast))
  (setvar "osmode" 2)
       (command "insert" "dif")
  (setvar "osmode" 0)
       (command "erase" ob1))

(defun C:E()
       (setvar "cmdecho" 0)
       (princ " = ERASE")
       (command "erase")(prin1))

(defun C:EW  ()
       (princ " = ERASE - Window")
       (command "ERASE" "W")(prin1))

(defun C:ECC  ()
       (princ " = ERASE - Crossing")
       (command "erase" "c")(prin1))

(defun C:EL  ()
       (princ " = ERASE - Last")
       (command "ERASE" "L" "")(prin1))

(defun C:EP(/ ss sno no x)
       (setvar "cmdecho" 0)
       (princ " = EXPLODE")
       (setq ss (ssget))
       (setq sno (sslength ss))
       (setq no 0)
       (repeat sno
           (setq x (ssname ss no))
           (command "explode" x)
           (setq no (1+ no))
       )(prin1))

(defun C:EX()
       (setvar "cmdecho" 0)
       (princ " = EXTEND")
       (command "extend")(prin1))

(defun C:F()
       (setvar "cmdecho" 0)
       (princ " = FILLET")
       (command "fillet")(prin1))

(defun C:FCC()
       (setvar "cmdecho" 0) 
       (princ " = FILLET - CROSSING")
       (command "fillet" "crossing")(prin1))

(defun C:L()
       (setvar "cmdecho" 0)
       (princ " = LINE")
       (command "line")(prin1))

(defun C:LO()
       (setvar "cmdecho" 0)
       (princ " = layer/on/all")
       (command "layer" "on" "*" "")(prin1))

(defun C:LE()
       (setvar "cmdecho" 0)
       (setvar "osmode" 1)
       (princ " = LINE - ENDP ")
       (command "line")(prin1)) 

(defun c:LN()
       (setvar "cmdecho" 0)
       (princ " = LINE - INT ")
       (command "line" "int")(prin1))
 
(defun c:LM()
       (setvar "cmdecho" 0)
       (princ "=line -mid ")
       (command "line"  "mid")(prin1))

(defun c:LR()
       (setvar "cmdecho" 0)
       (PRINC "=LINE -NEAR")
       (COMMAND "LINE"  "NEAR")(PRIN1))

(defun c:LU()
       (setver "cmdecho" 0)
       (princ "=line -quad")
       (command "line"  "quad")(prin2))

(defun C:LI()
       (setvar "cmdecho" 0)
       (princ " = LIST")
       (command "list")(prin1))

(DEFUN C:LS()
       (setvar "cmdecho" 0)
       (princ " = LAYER / SET")
       (command "layer" "set") (prin1))

(defun C:LT ()
      (princ " = LINETYPE")
      (command "LINETYPE")(prin1))

(defun C:LTS ()
      (princ " = LTSCALE")
      (command "ltscale")(prin1))

(defun C:M()
       (setvar "cmdecho" 0)
       (princ " = MOVE")
       (command "move")(prin1))
(defun C:ML  ()
       (princ " = MOVE - Last")
       (command "MOVE" "L" "")(prin1))
(defun C:MCC()
       (princ " = MOVE - CROSSING")
       (command "MOVE" "C")(prin1))
(defun C:MW()
       (princ " = MOVE - Window")
       (command "MOVE" "W")(prin1))
(defun C:ME (/ CE SS LS NO PNT1 PNT2)
       (setvar "CMDECHO" 0)
       (prompt "Select extend edge")(terpri)
       (setq CE (ssget))(terpri)
       (prompt "Select objects to extend:")
         (terpri)
       (setq PNT1 (getpoint "First point"))
         (terpri)
       (setq PNT2 (getcorner PNT1 "2nd pnt"))
       (terpri)
       (setq SS (ssget "C" PNT1 PNT2)
             LS (sslength SS)
             NO -1
       )
       (command "EXTEND" CE "")
       (repeat LS
       (setq NO (1+ NO))
       (command (list (ssname SS NO) PNT2))
       )
       (command "")
       (setvar "CMDECHO" 1)
       (prin1))

(defun C:MI()
       (setvar "mirrtext" 0)
       (princ " = MIRROR")
       (command "mirror")(prin1))

(defun C:MIW()
       (setvar "mirrtext" 0)
       (princ " = mirror - window")
       (command "mirror" "window")(prin1))

(defun C:MT (/ CE SS LS NO PNT1 PNT2)
       (setvar "CMDECHO" 0)
       (prompt "Select cutting edge")(terpri)
       (setq CE (ssget))(terpri)
       (prompt "Select objects to trim:")
         (terpri)
       (setq PNT1 (getpoint "First point"))
         (terpri)
       (setq PNT2 (getcorner PNT1 "2nd pnt"))
         (terpri)
       (setq SS (ssget "C" PNT1 PNT2)
             LS (sslength SS)
             NO -1
       )
       (command "TRIM" CE "")
       (repeat LS
       (setq NO (1+ NO))
       (command (list (ssname SS NO) PNT2))
       )
       (command "")
       (setvar "CMDECHO" 1)
       (prin1))

(DEFUN C:N()
       (setvar "cmdecho" 0)
       (princ " = OSNAP / NONE")
       (command "osnap" "none") (prin1))

(defun C:O()
       (setvar "cmdecho" 0)
       (princ " = OFFSET")
       (command "offset")(prin1))

(defun C:OO(/ z pt ss Q:LA)
       (setvar "cmdecho" 0)
       (princ " = OFFSET & CHANGE LAYER TO CURRENT..")
       (setq Q:LA (getvar "clayer"))
       (if Q:ODIST
           (progn
               (prompt "\nOffset distance or Through <")
               (princ Q:ODIST)
               (princ "> : ")
               (setq z (getdist))
               (if z
                   (setq Q:ODIST z)
               )
           )
           (progn
               (prompt "\nOffset distance or Through <Through> : ")
               (initget (+ 1 2 4))
               (setq Q:ODIST (getdist))
           )
       )
       (setq ss (entsel "\nSelect object to offset : "))
       (while ss
              (setq pt (getpoint "\nSide to offset? "))
              (command "offset" (setq z Q:ODIST) 
                       (setq z (cadr ss)) (setq z pt) "")
              (command "change" "L" "" "P" "la" (setq z Q:LA) "")
              (setq ss (entsel "\nSelect object to offset : "))
       )
       (setvar "cmdecho" 1)
       (prin1))

(DEFUN C:ON()
       (setvar "cmdecho" 0)
       (princ " = LAYER / ON")
       (command "layer" "on") (prin1))

(defun C:ona  ()
       (princ " = LAYER - on - all")
       (command "LAYER" "on" "*" "" )(prin1))

(DEFUN C:OFF()
       (setvar "cmdecho" 0)
       (princ " = LAYER / OFF")
       (command "layer" "off") (prin1))

(defun C:offa  ()
       (princ " = LAYER - off - all")
       (command "LAYER" "off" "*" "" )(prin1))

(DEFUN C:OE()
       (setvar "cmdecho" 0)
       (princ " = OSNAP / END")
       (command "osnap" "end") (prin1))

(DEFUN C:OM()
       (setvar "cmdecho" 0)
       (princ " = OSNAP / MID")
       (command "osnap" "mid") (prin1))

(DEFUN C:OI()
       (setvar "cmdecho" 0)
       (princ " = OSNAP / INT")
       (command "osnap" "int") (prin1))

(DEFUN C:OIP()
       (setvar "cmdecho" 0)
       (princ " = OSNAP / INT / PER")
       (command "osnap" "int,per") (prin1))

(DEFUN C:ONP()
       (setvar "cmdecho" 0)
       (princ " = OSNAP / NEA / PER")
       (command "osnap" "nea,per") (prin1))

(defun C:PE()
       (setvar "cmdecho" 0)
       (princ " = PEDIT")
       (command "pedit")(prin1))

(defun C:PL()
       (setvar "cmdecho" 0)
       (princ " = PLINE")
       (command "pline")(prin1))

(defun C:Pg()
       (setvar "cmdecho" 0)
       (command "style" "ks" "simplex,ghs" "" "" "" "" "" "")
       (command "limits" "0,0" "841,594")
       (princ " = purge/all")
       (command "purge" "a" "" "n")
       (command "qsave")(prin1))

(defun C:R()
       (setvar "cmdecho" 0)
       (princ " = Redraw")
       (command "redraw")(prin1))

(defun c:ro()
       (setvar "cmdecho"0)
       (princ " =ROTATE")
       (COMMAND "rotate")(prin1))

(defun C:Rw()
       (setvar "cmdecho" 0)
       (princ " = ROTATE - WINDOW")
       (command "rotate" "w")(prin1))
     
(defun C:RC()
       (setvar "cmdecho" 0)
       (princ " = ROTATE - CROSSING")
       (command "rotate" "c")(prin1))       
 
(defun C:S()
       (setvar "cmdecho" 0)
       (princ " = STRETCH")
       (command "stretch" "c")(prin1))

(defun C:SC()
       (setvar "cmdecho" 0)
       (princ " = SCALE")
       (command "scale")(prin1))
(defun C:Sl()
       (setvar "cmdecho" 0)
       (princ " = STYLE")
       (command "STYLE")(prin1))

;;duct spin in branch .lsp
;;2003.02.22 ohjongyel
;;2003.05.08 수정

(defun c:sp()
  (setvar "cmdecho" 0)
  (setvar "osmode" 512)
  (setq p0 (getpoint "\n스핀인을 삽입할 닥트 선택:"))
  (setvar "osmode" 0)
  (setvar "orthomode" 1)
  (setq an (getangle p0 "\n스핀인의 방향 지정:"))
  (setq ang (rtd an))
  (setvar "orthomode" 0)
  (setq p1 (polar p0 (dtr 270) 100))
  (setq p2 (polar p1 (dtr 0) 100))
  (setq p3 (polar p2 (dtr 90) 200))
  (setq p4 (polar p3 (dtr 180) 100))
    (command "pline" p1 p2 p3 p4 "")
    (command "rotate" "l" "" p0 ang)
(princ)
  (setvar "osmode" 36)
  (setq pl1 (getpoint "\n디퓨져의 가운데지점을 선택하세요:"))
  (setvar "osmode" 0)
  (setq pl2 (getpoint pl1 "\n두번째점을 선택하세요:"))
  (setvar "osmode" 128)
  (setq pl3 (getpoint pl2 "\n스핀인을 지정하세요:"))
  (setvar "osmode" 0)
  (command "pline" pl1 pl2 pl3 "")
  (command "fillet" "r" "100")
  (command "fillet" "p" "l")
(princ)
)

(defun C:T()
       (setvar "cmdecho" 0)
       (princ " = TRIM")
       (command "trim")(prin1))

(defun C:TCC()
       (princ " = TRIM - CROSSING")
       (command "trim" "crossing")(prin1))  

(defun C:WB()
       (setvar "cmdecho" 0)
       (princ " = WBLOCK")
       (command "wblock")(prin1))

(defun C:Z()
       (setvar "cmdecho" 0)
       (princ " = ZOOM")
       (command "zoom")(prin1))

(DEFUN C:ZA()
       (setvar "cmdecho" 0)
       (princ " = ZOOM / ALL")
       (command "zoom" "a") (prin1))

(DEFUN C:ZD()
       (setvar "cmdecho" 0)
       (princ " = ZOOM / DYNAMIC")
       (command "zoom" "d") (prin1))

(DEFUN C:ZE()
       (setvar "cmdecho" 0)
       (princ " = ZOOM / EXTEND")
       (command "zoom" "e") (prin1))

(DEFUN C:ZP()
       (setvar "cmdecho" 0)
       (princ " = ZOOM / PREVIOUS")
       (command "zoom" "p") (prin1))

(DEFUN C:ZW()
       (setvar "cmdecho" 0)
       (princ " = ZOOM / WINDOW")
       (command "zoom" "w") (prin1))

(DEFUN C:QB()
  (SETQ A (GETSTRING 
     "\nBlock Name or ?:"))
  (IF (= A "?") (LISTBLOCK) (MAKEBLOCK)
  )
  (PRINC)
)

(DEFUN LISTBLOCK()
  (COMMAND "BLOCK" "?")
)

(DEFUN MAKEBLOCK()
  (SETQ B (TBLSEARCH "BLOCK" A))
  (SETQ C "N")
  (IF B 
    (PROGN
    (PRINC "\nBlock already exists.")
    (SETQ C (STRCASE (GETSTRING 
      "\nRedefine it (Y/N)? ")))
    )
  )
  (IF (OR (= "Y" C) (NOT B)) 
    (PROGN
    (SETVAR "EXPERT" 2)
    (MENUCMD "S=OSNAPB")
    (SETQ D (GETPOINT 
      "\nInsertion Base Point: "))
    (MENUCMD "S=SELECT")
    (SETQ E (SSGET))
    (MENUCMD "S=")
    (COMMAND "BLOCK" A D E "")
    (COMMAND "INSERT" A D 1 1 0)
    (SETVAR "EXPERT" 0)
    )
  )
)

(VMON)
(defun dtr (a)
  (* pi (/ a 180.0))
)
;
(defun rtd (a)
  (/ (* a 180.0) pi)
)

(defun c:v1()
       (setvar "cmdecho" 0)
       (princ " = tilemode-0 paperspace A1 SIZE로 만듭니다.")
       (command "psltscale" "0")
       (command "layer" "m" "tile0" "c" "7" "" "")
       (command "color" "bylayer" "")
       (command "tilemode" "0")
       (command "limits" "0,0" "841,594")
       (command "mview" "0,0" "841,594")
       (command "mspace")
       (command "zoom" "1/50xp")
       (command "pspace")
 (princ "zoom-extend")
       (command "zoom" "e")
       (command "style" "unist0" "simplex,ghs" "" "" "" "" "" "")
       (command "style" "ghs" "simplex,ghs" "" "" "" "" "" "")
       (command "style" "standard" "simplex,ghs" "" "" "" "" "" "")
       (command "style" "휴먼고딕" "simplex,ghs" "" "" "" "" "" "")
       (command "style" "style" "simplex,ghs" "" "" "" "" "" "")
       (command "style" "style0" "simplex,ghs" "" "" "" "" "" "")
       (command "style" "style1" "simplex,ghs" "" "" "" "" "" "")
       (command "style" "style2" "simplex,ghs" "" "" "" "" "" "")
)

(defun c:v()
       (setvar "cmdecho" 0)
       (princ " = tilemode-0 paperspace A0 SIZE로 만듭니다.")
       (command "psltscale" "0")
       (command "layer" "m" "tile0" "c" "7" "" "")
       (command "color" "bylayer" "")
       (command "tilemode" "0")
       (command "limits" "0,0" "1188,841")
       (command "mview" "0,0" "1188,841")
       (command "mspace")
       (command "zoom" "1/200xp")
       (command "pspace")
 (princ "zoom-extend")
       (command "zoom" "e")
       (command "style" "hs" "simplex,hs2" "" "" "" "" "" "")
       (command "style" "hh" "simplex,hs2" "" "" "" "" "" "")
       (command "style" "hsh" "simplex,ghs2" "" "" "" "" "" "")
       (command "style" "cgs" "simplex,CGS" "" "" "" "" "" "")
 (princ "purge-all")
       (command "purge" "a" "" "n")
       (command "qsave")
 (prin1)
       (command "open" "~")
 (prin1)
)

(defun c:v3()
       (setvar "cmdecho" 0)
       (princ " = tilemode-0 paperspace A3 SIZE로 만듭니다.")
       (command "psltscale" "0")
       (command "layer" "m" "tile0" "c" "7" "" "")
       (command "color" "bylayer" "")
       (command "tilemode" "0")
       (command "limits" "0,0" "420,297")
       (command "mview" "0,0" "420,297")
       (command "mspace")
       (command "zoom" "1/200xp")
       (command "pspace")
 (princ "zoom-extend")
       (command "zoom" "e")
       (command "style" "gsg1" "simplex,ghs" "" "" "" "" "" "")
       (princ "purge-all")
       (command "purge" "a" "" "n")
       (command "qsave")
 (prin1)
       (command "open" "~")
)

(DEFUN C:mb(/ Qj Q@ QQ Ql Q& Q1)
 (F:INIT(quote ("osmode")))
 (SETVAR"osmode"0)
 (SETQ Qj(GETPOINT"\n절단할 첫번째 구석 : ")
       Q@(GETCORNER Qj"\n절단할 반대 구석 : "))
 (COMMAND"_.ERASE""w"Qj Q@"")
 (SETQ Q#(SSGET"c"Qj Q@)
       Q1(SSLENGTH Q#)
       Q0 0)
 (REPEAT Q1(SETQ Q$(SSNAME Q# Q0)
                 Q0(1+ Q0))
 (COMMAND"_.BREAK"Q$ Qj Q@))
 (F:END)
)
 (PRINC)


;UDIST User interface distance function
(defun udist (bit kwd msg def bpt / inp)
  (if def
    (setq msg (strcat "\n" msg " <" (rtos def) ">: ")
          bit (* 2 (fix (/ bit 2)))
    )
    (setq msg (strcat "\n" msg ": "))
  )
  (initget bit kwd)
  (setq inp
    (if bpt
       (getdist msg bpt)
       (getdist msg)
    )
  )
  (if inp inp def)
)  
(defun upoint (bit kwd msg def bpt / inp)
   (if def 
      (setq pts (strcat (rtos (car def)) "," (rtos (cadr def))
                        (if (and (caddr def) (= 0 (getvar "flatland")))
                            (strcat "," (rtos (caddr def)))
                            ""
                         )
                 )
             msg (strcat "\n" msg " <" pts ">: ")
             bit (* 2 (fix (/ bit 2)))
       )
       (setq msg (strcat "\n" msg ": "))
    )
    (initget bit kwd)
    (setq inp (if bpt
                  (getpoint msg bpt)
                  (getpoint msg)
              )
    )
    (if inp inp def)
);def upoint
;*********************************************************
;     LINE OFFSET program
;     programmer : Choi Jin-Kook
;     date       : 1990. 2. 2     
;********************************************************* 
(defun C:MF ( / ent spt tdist dist)
   (setq tdist 0
         ent (entsel "\nSelect line to offset : ")
         spt (upoint 1 "" "Which side" nil (cadr ent))
   )
   (while (and
            (setq dist (udist 1 "" "\nOffset distance (if END=0) ? " dist nil))
            (/= dist 0.0))
        (setq tdist (+ tdist dist))
        (command "offset" tdist ent spt "")
   )
   (princ)
)
 (PRINC)
;;kbh---
(setq env_var "C:\\success\\KBH\\")
(load (strcat env_var "kbh"))