require 'rubygems'
require 'rake'
require './lib/pprb'

begin
  require 'jeweler'
  
  Jeweler::Tasks.new do |gem|
    gem.name = "pprb"
    gem.authors = ["Zahary Karadjov", "Stefan Dragnev"]
    gem.summary = "PPRB The little preprocessor that could"
    gem.description = "PPRB is a code preprocessor and text templating engine with minimalistic syntax and strong emphasis on eliminating any code duplication in template files"
    gem.email = "zahary@gmail.com"
    gem.homepage = "http://github.com/zah/pprb"    
    gem.version = PPRB::Version::STRING
    
    gem.executable = 'pprb'
    
    gem.files.exclude 'CMake'
    gem.files.exclude 'VisualStudio'
    
    gem.add_development_dependency "thoughtbot-shoulda"
    gem.add_dependency "trollop"    
  end
  
  #Jeweler::GemcutterTasks.new
rescue LoadError
  puts "Jeweler (or a dependency) not available. Install it with: sudo gem install jeweler"
end

require 'rake/testtask'
Rake::TestTask.new(:test) do |test|
  test.libs << 'lib' << 'test'
  test.pattern = 'test/**/*_test.rb'
  test.verbose = true
end

begin
  require 'rcov/rcovtask'
  Rcov::RcovTask.new do |test|
    test.libs << 'test'
    test.pattern = 'test/**/*_test.rb'
    test.verbose = true
  end
rescue LoadError
  task :rcov do
    abort "RCov is not available. In order to run rcov, you must: sudo gem install spicycode-rcov"
  end
end

task :test => :check_dependencies

task :default => :test

require 'rake/rdoctask'
Rake::RDocTask.new do |rdoc|
  if File.exist?('VERSION')
    version = File.read('VERSION')
  else
    version = ""
  end

  rdoc.rdoc_dir = 'rdoc'
  rdoc.title = "pprb #{version}"
  rdoc.rdoc_files.include('README*')
  rdoc.rdoc_files.include('lib/**/*.rb')
end
