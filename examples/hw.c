> hw = "Hello World"

% rb
  puts "Arbitrary ruby code is executed here"
  puts "Functions could be defined"

  def test a, b
    a + b
  end 
-

> # this defines a function, but keeps the standard mode for 
> # "lines without special symbols are output"
% def hello_world_template
  puts("Hello World")
  % 3.times
  puts("Hello Loop")
  -
-

void main()
{
  > hello_world_template
  
  % 6.times
  printf("%d", `test 5, 10`);
  -
}

