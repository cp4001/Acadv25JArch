;;;;;;;;;extend muiltple
(defun em(/ ce www is no pn1 pn2)
    (setvar "cmdecho" 0)
    (prompt "Select cutting edge")(terpri)
    (setq ce (ssget))(terpri)
    (prompt "Select objects to trim:")(terpri)
    (setq pn1 (getpoint "First point"))(terpri)
    (setq pn2 (getcorner pn1 "Sencod point"))(terpri)
    (setq www(ssget "c" pn1 pn2)
        is (sslength www)
        no -1
    )
    (command "extend" ce "")
    (repeat is
    (setq no(1+ no))
    (command (list (ssname www no) pn2))
    )
    (command "")
    (setvar "cmdecho" 1)
)
;;;;;;;;;extend muiltple
(defun C:mt(/ ce www is no pn1 pn2)
    (setvar "cmdecho" 0)
    (prompt "Select cutting edge")(terpri)
    (setq ce (ssget))(terpri)
    (prompt "Select objects to trim:")(terpri)
    (setq pn1 (getpoint "First point"))(terpri)
    (setq pn2 (getcorner pn1 "Sencod point"))(terpri)
    (setq www(ssget "c" pn1 pn2)
        is (sslength www)
        no -1
    )
    (command "trim" ce "")
    (repeat is
        (setq no(1+ no))
        (command (list (ssname www no) pn2))
    )
    (command "")
    (setvar "cmdecho" 1)
)    

;-------- ERROR PART START -------------
(defun jhgerr (jjj)
    (if (/= jjj "\nFunction cancelled")
        (princ (strcat "\nError : " jjj))
    )
;   (setvar "highlight" jodh)
    (setvar "cmdecho" jodcm)
    (setvar "orthomode" jodoth)
    (setq *error* olderr)
    (prompt "\nPlease again loading < J.H.G Multi Trim : JMT >.")
    (princ)
)
;-------- ERROR PART END ---------------


;-------- MAIN PART START --------------
(defun jmultitrim(/ olderr jpt1 jpt2 jssg jssnum jindexn jent jentstart
                      jentend jinterpt jodh jodcm jodoth)
(graphscr)
(prompt "\nLine Multi Trim < JMT > program loading.")

(setq olderr *error* *error* jhgerr
;     jodh (getvar "highlight")
      jodcm (getvar "cmdecho")
      jodoth (getvar "orthomode")
)

(command "cmdecho" 0)
;(command "highlight" 0)
(command "orthomode" 1)

(initget 1)
(setq jpt1 (getpoint "\nPick multi trim first point : "))
(initget 1)
(setq jpt2 (getcorner jpt1 "\nPick multi tirm 2nd point : "))

(setq jssg (ssget "c" jpt1 jpt2)
      jssnum (sslength jssg)
      jindexn 0
)

(repeat jssnum
  (progn
       (setq jent (entget (ssname jssg jindexn))
             jindexn (1+ jindexn)
             jentstart (cdr (assoc 10 jent))
             jentend   (cdr (assoc 11 jent))
             jinterpt  (inters jpt1 jpt2 jentstart jentend)
       )
       (command "trim" "C" jentstart jentend "" jinterpt "")
  );end progn
)

;(setvar "highlight" jodh)
(setvar "cmdecho" jodcm)
(setvar "orthomode" jodoth)

(prompt "\nJ.H.G line Multi Trim < JMT > program complete.")
(setq *error* olderr)
(princ)
)
;----------- MAIN PART END ------------------

(defun C:tm()(vmon)(jmultitrim)(princ))
(princ "\nJ.H.G : Multi Trim loaded.  Start command with JMT.")
(princ)


(defun pipetrim(/ la jent1 strent1 pnt1 pnt2 ll ia strent gsrt gend ang1
               selent l i jent1 jen esrt eend gint ang p1 p2 fst1 fst2
               scd1 scd2 )
    (graphscr)
    (setq os(getvar "osmode"))
    (command "OSMODE" 0)
    (command "cmdecho" 0)
    (command "orthomode" 1)
    (initget 1)
    (setq jent1 nil)
    (terpri)
    (prompt "------  Main Pipe  ------")(terpri)
    (setq strent1 (ssget))(terpri)
    (terpri)
    (prompt "------  Seb Pipe  ------")(terpri)
    (setq pnt1 (getpoint "\nFirst corner of Seb pipe "))
    (setq pnt2 (getcorner pnt1 "\nSeccond corner of Seb pipe"))
    (setq ll (sslength strent1))
    (setq ia 0)
    (repeat ll
        (setq strent (entget (ssname strent1 ia)))
        (setq gsrt (cdr (assoc 10 strent))
              gend (cdr (assoc 11 strent))
        )
        (setq ang1  (angle gsrt gend)
              gsrt (polar gsrt (+ ang1 (dtr 90)) (/ s 2))
              gend (polar gend (+ ang1 (dtr 90)) (/ s 2))
              selent (ssget "c" gsrt gend)
              gsrt (polar gsrt (+ ang1 (dtr 270)) (/ s 2))
              gend (polar gend (+ ang1 (dtr 270)) (/ s 2))
              l (sslength selent)
              i 0
        )
        (repeat l
            (setq jent1 (ssname selent i)
                  jent (entget (ssname selent i))
            )
            (setq jjent(cdr (assoc 0 jent)))
            (if (or (= "LINE" jjent) (= "POLYLINE" jjent))
                (progn
                    (setq esrt (cdr (assoc 10 jent))
                          eend (cdr (assoc 11 jent))
                          gint (inters gsrt gend esrt eend)
                          ang  (angle esrt eend)
                          p1 (polar gint ang s)
                          p2 (polar gint (+ ang (dtr 180)) s)
                    )
                    (if (> (car pnt1) (car pnt2))
                       (progn (setq fst1(car pnt1)) (setq fst2(car pnt2)))
                       (progn (setq fst1(car pnt2)) (setq fst2(car pnt1)))
                    )
                    (if (> (cadr pnt1) (cadr pnt2))
                       (progn (setq scd1(cadr pnt1)) (setq scd2(cadr pnt2)))
                       (progn (setq scd1(cadr pnt2)) (setq scd2(cadr pnt1)))
                    )
                    (if (and (< fst2 (car gint)) (< scd2 (cadr gint)) (> fst1 (car gint)) (> scd1 (cadr gint)))
                       (command "break" jent1 p1 p2)
                    )
                )
            )
            (setq i (+ i 1))
        )
        (setq ia (+ ia 1))
    )
    (setvar "osmode" os)
)
