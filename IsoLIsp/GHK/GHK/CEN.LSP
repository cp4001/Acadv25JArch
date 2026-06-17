(vmon)
;-------- ERROR PART START -------------
(defun jhgerr (jjj)
    (if (/= jjj "\nFunction cancelled")
        (princ (strcat "\nError : " jjj))
    )
    (command "layer" "s" la "")
    (setvar "cmdecho" jodcm)
    (setq *error* olderr olderr nil jodcm nil jo-ltype nil jo-color ni jindex nil)
)
;-------- ERROR PART END ---------------


;-------- MAIN PART START --------------
(defun c:cen(/ jg-ent jg-ent10 jg-ent40 js-pt1 js-pt2 js-pt3 js-pt4
                  jg-ent0 jcount)
    (graphscr)
    (setq olderr *error* *error* jhgerr
          jodcm     (getvar "cmdecho")
    )
    (princ "\n")
    (command "cmdecho" 0)
    (setq la (getvar "clayer"))
    (layer1)
    (command "layer" "s" "1e" "")
    (setq os (getvar "osmode"))
    (setvar "osmode" 0)
    (prompt "\nSelect circle Multi center line : ")
    (setq jg-ssget  (ssget))
    (if (/= jg-ssget nil)
        (progn
            (setq jg-number (sslength jg-ssget)
                  jindex 0
                  jcount 0
            )
            (repeat jg-number
                (setq jg-ent  (entget (ssname jg-ssget jindex))
                      jg-ent0 (cdr (assoc 0 jg-ent))
                      jindex  (1+ jindex)
                )
                (if (or (= jg-ent0 "CIRCLE") (= jg-ent0 "ARC"))
                    (progn
                     (setq jg-ent10 (cdr (assoc 10 jg-ent))
                           jg-ent40 (* (cdr (assoc 40 jg-ent)) 1.4)
                           js-pt1   (list (- (car jg-ent10) jg-ent40) (cadr jg-ent10))
                           js-pt2   (list (+ (car jg-ent10) jg-ent40) (cadr jg-ent10))
                           js-pt3   (list (car jg-ent10) (- (cadr jg-ent10) jg-ent40))
                           js-pt4   (list (car jg-ent10) (+ (cadr jg-ent10) jg-ent40))
                           jcount (1+ jcount)
                     )
                     (command "line" js-pt1 js-pt2 "" "line" js-pt3 js-pt4 "")
                    )
                )
            )
        )
    )
    (command "layer" "s" la "")
    (setvar "osmode" os)
)
        ;----------- MAIN PART END ------------
