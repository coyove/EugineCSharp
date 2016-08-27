(println "Begin tests\n")

[start = (time 1)]

(~include "util.lisp")

(println "== Mathematics ==")	
(~include "math.lisp")

(println "\n== Lambda / Recursion ==") 
(~include "lambda.lisp")
(~include "recursion.lisp")

(println "\n== Others ==") 
(~include "others.lisp")

; (println "Benchmark") (include "bench.lisp")

(println "\nFinish tests in " (floor [(time 1) - start]) "ms")