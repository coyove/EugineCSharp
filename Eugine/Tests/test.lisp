;; if you tend to use a var without assign a value to it, its value = null
;; if you use a var with a "@" before its name (no space between), like @var-name, its value = "var-name" (string)

(println "Begin tests")
(println "Working under " ~path "\n")

; (set ~strict)

(var start)
[start = (time 1)]

(~include "util.lisp")

(println "== Mathematics ==")	
(~include "math.lisp")

(println "\n== Lambda / Recursion ==") 
(~include "lambda.lisp")
(~include "recursion.lisp")

(println "\n== Others ==") 
(~include "others.lisp")

(~include "interop.eugine")

(println "\nFinish tests in " (floor [(time 1) - start]) "ms")