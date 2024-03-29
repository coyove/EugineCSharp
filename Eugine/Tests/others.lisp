(assert [[(head (list)) == null] and [(== null null null 1) == #f]] "Null equality")

;========================================

(defun last-elem (lst) (head (sub lst [(len lst) - 1])))

(set ending (floor [(random 0) * 1000]))

(assert [(last-elem (range 0 [ending + 1])) == ending] "Last elem: " ending)

;========================================

(defun zip-list (lst) 
	"zip multiple lists"
	(->
		(if [[(type lst) == "list"] and [(len lst) > 0]]
			(->
				[ret = (list)]
				(for (range 0 (len [lst : 0])) (lambda (i) (->
					[newlist = (list)]
					(for (range 0 (len lst)) (lambda (j) (->
						[newlist += [lst : j i]]
					)))

					[ret += newlist]
				)))	
				(ret)
			)

			(-> (exit "invalid list"))
		)
	)
)

(set #zip-list ((1 5 8) (2 4 6) (3 7 9)))
(assert	[(zip-list #zip-list) deep-compare-list ((1 2 3) (5 4 7) (8 6 9))] "Zip list: " #zip-list)

;========================================

(set convert-weekday (lambda (i) (->
	(cond (i) 
		(1 "Mon")
		((/ 4 2) "Tue")
		(3 "Wed")
		(4 "Thu")
		(5 "Fri")
		(6 "Sat")
		(7 "Sun")
		(_ "-")
	)
)))

(set result ())
(for (1 3 5 9 6 8 7 5 4 2) [(i) => (
	[result += (convert-weekday i)]
)])

(assert (deep-compare-list result ("Mon" "Wed" "Fri" "-" "Sat" "-" "Sun" "Fri" "Thu" "Tue")) "Week: " result)

;========================================

(set #test-string "你好, 世界 - こんにちわ、この世界")
(assert [(split #test-string ()) 
	deep-compare-list 
	("你" "好" "," " " "世" "界" " " "-" " " "こ" "ん" "に" "ち" "わ" "、" "こ" "の" "世" "界")] 
	"Unicode string comparison 1")

(assert [(sub #test-string 4 2) == (sub #test-string 17 2)] "Unicode string comparison 2")
(assert [(+ (explode (chr (asc (split #test-string ()))))) == #test-string] "Unicode string comparison 3")

;========================================

(assert [@"hello world 你好, 世界" == "hello world 你好, 世界"] "Literal string 1")
(assert [@"hello world ""你好"", 世界" == "hello world \"你好\", 世界"] "Literal string 2")

;========================================

[math-op = (dict)]
[[math-op : "add"] = (list [(a b) => (+ a b)] (1 2 3 4 5) (5 4 3 2 1) ( 6    6  6    6 6))]
[[math-op : "sub"] = (list [(a b) => (- a b)] (2 3 4 5 6) (5 4 3 2 1) (-3   -1  1    3 5))]
[[math-op : "div"] = (list [(a b) => (/ a b)] (3 4 5 6 7) (5 4 2 2 1) ( 0.6  1  2.5  3 7))]
[[math-op : "mul"] = (list [(a b) => (* a b)] (4 5 6 7 8) (5 4 3 2 1) (20   20 18   14 8))]
[[math-op : "mod"] = (list [(a b) => (% a b)] (5 6 7 8 9) (5 4 3 2 1) ( 0    2  1    0 0))]

[list-op = [(op list-1 list-2) => (->
	[ret = (list)]
	(loop list-1 [(e i) => [ret += (op e [list-2 : i])]])
	(ret)
)]]

(loop (keys math-op) [(k) => (
	(assert (deep-compare-list (list-op [math-op : k 0] [math-op : k 1] [math-op : k 2]) 
		[[math-op : k] : 3]) "Currying " k)
)])

;========================================

(set re (regex @"https?:\/\/(\S+)" @IgnoreCase))
(set #regex-test-string "Google's website should be http://www.google.com or
HTTPS://google.com if you regard safe-browsing as important")

;; as long as the .NET regex engine is working properly, these should be correct too
(assert [[(match re #regex-test-string) : "1" "capture"] == "www.google.com"] "regex test 1")
(assert [[(match re #regex-test-string 40) : "0" "capture"] == "HTTPS://google.com"] "regex test 2")

;========================================

[lt = [(l r) => (> l r)]]
[gte = [(l r) => (<= l r)]]

(defun quick-sort (lst)
	"Slowest quick sort using recursion"
	(cond #t
		([(len lst) == 0] (list))
		([(len lst) == 1] (list (head lst)))
		(_ (-> 
			[#head = (head lst)]
			[#tail = (tail lst)]
			(+ (quick-sort (filter (lt #head) #tail))
				#head 
				(explode (quick-sort (filter (gte #head) #tail)))
			)
		))
	)
)

(defun quick-sort-2 (arr) (->
	"A bit faster"
	[swap = [(a b) => (->
		[tmp = [arr : a]]
		[[arr : a] = [arr : b]]
		[[arr : b] = tmp]
	)]]

	[sort = [(beg end) => (if [end > [beg + 1]] (->
		[p = [arr : beg]] 
		[l = [beg + 1]]
		[r = end]

	    (loop [l < r] [() => (
	    	(if [[arr : l] <= p] 
	    		[l ++]
	        	(swap l [r --])
	    	)
		)])
	    
	    (swap [l --] beg)

	    (sort beg l)
	    (sort r end)
	))]]

	(sort 0 (len arr))
	(arr)
))

(defun quick-sort-3 (arr) (->
	"Got rid of recursion"
	[swap = [(a b) => (->
		[tmp = [arr : a]]
		[[arr : a] = [arr : b]]
		[[arr : b] = tmp]
	)]]
 
	[partition = [(l h) => (->
		[x = [arr : h]]
	    [i = [l - 1]]
	 
	    ;for (int j = l; j <= h- 1; j++)
	    (loop (range l h) [(j) =>
	    	(if [[arr : j] <= x] (swap [i ++] j))
    	])
	    
	    (swap [i + 1] h)
	    [i + 1]
	)]]

    ;int stack[ h - l + 1 ];
    [h = [(len arr) - 1]]
    [l = 0]
    [stack = (range 0 (len arr))]
    ;[loop (range 0 [[h - l] + 1]) [() => [stack += 0]]]
 
    [[stack : 0] = l]
    [[stack : 1] = h]
    [top = 1] 
 	
    (loop [top >= 0] [() => (->
        [h = [stack : top]]
        [top --]
        [l = [stack : top]]
        [top --]
 
        [p = (partition l h)]
 
        (if [[p - 1] > l] (->
        	[[stack : [top ++]] = l]
            [[stack : [top ++]] = [p - 1]]
    	))
 
        (if [[p + 1] < h] (->
            [[stack : [top ++]] = [p + 1]]
            [[stack : [top ++]] = h]
        ))
	)])

	(arr)
))

(assert [(quick-sort (random-list 100)) deep-compare-list (range 0 100)] "Quick sort 1")
(assert [(quick-sort-2 (random-list 100)) deep-compare-list (range 0 100)] "Quick sort 2")
(assert [(quick-sort-3 (random-list 100)) deep-compare-list (range 0 100)] "Quick sort 3")