module PPRB
  module Version
    MAJOR = 0
    MINOR = 9
    PATCH = 1
    
    STRING = [MAJOR, MINOR, PATCH].compact.join('.')
  end
  
  DEFAULT_RULES_FILE = "pprb.rules"
  
  def self.render source, rules_file
    PPRB.new(source, :pp, rules_file).run
  end

  def self.compile source
    PPRB.new(source)
  end
  
  class PPRB
    # source is either a File object or a string
    # start_state is either :pp or :rb
    def initialize source, start_state = :pp, rules_file = nil
      @source = source
      @path = File.expand_path(source.path) if source.class == File

      @BLOCK = @LINE = @END = @TICKS = ''
      profile :default

      @ruby_output = ''
      @target_output = ''

      @input_state = [start_state]
      @last_emitted = :code

      load_rules(rules_file ? rules_file : DEFAULT_RULES_FILE)
      
      source.lines.each { |l| process_line l }
      @ruby_output << ']' if @last_emitted == :text
    end

    def run
      run_and_translate_errors @ruby_output
      @target_output
    end

    attr_reader :ruby_output, :target_output

    PROFILES = {
      :default => {
        :block => /^%(.*)/,
        :line  => /^>(.*)/,
        :end   => /^-$/,
        :ticks => /`(.*?)`/
      },

      :c_parsable => {
        :block => %r[^//\s*%(.*)],
        :line  => %r[^//\s*>(.*)],
        :end   => %r[^//\s*-$],
        :ticks => /\brb_(\w+?)_\b/
      },
    }

    def profile filter = nil, prof = nil, overlay = nil
      # sort-out the passed arguments
      overlay = filter  if filter.class == Hash
      overlay = prof    if prof.class == Hash
      prof    = filter  if filter.class == Symbol
      filter  = nil     if filter != nil and filter.class != String
      prof    = nil     if prof != nil and prof.class != Symbol

      raise "No profile or overlay specified" if overlay == nil and prof == nil

      unless filter
        if prof
          profile = PROFILES[prof]
          raise "Unknown profile: #{p}" if profile == nil
          config profile
        end

        config overlay if overlay
      else
        profile nil, prof, overlay if File.fnmatch(filter, @path)
      end
    end

    def config options
      for key, value in options
        var = "@#{key.to_s.upcase}"
        raise "Unknown option: #{key}" if instance_variable_get(var) == nil
        instance_variable_set var, value
      end
    end

  private
    def process_line line
      case line.strip
        when @BLOCK then begin_block $1+"\n"
        when @END   then end_block
        when @LINE  then emit $1+"\n", antistate(@input_state.last)
        else emit line, @input_state.last
      end
    end

    def end_block
      raise "Unexpected block end (no matching block start)." if @input_state.size == 1

      if @input_state.pop == :pp
        emit "end\n", :rb
      else
        emit '', :pp
      end
    end

    def begin_block line
      if @input_state.last == :pp
        case line
          when /^\s*rb/ then
            @input_state.push :rb
            emit '', :pp
          when /\bdef|\bfor\b|\bwhile\b|\buntil\b|\bdo\b|\bif\b|\bunless\b|\case\b|\bclass\b|\bmodule\b|\bbegin\b/ then
            @input_state.push :pp
            emit line, :rb
          else
            @input_state.push :pp
            emit line.rstrip + " do\n", :rb
        end
      else # state :rb
        @input_state.push :pp
        emit "begin ", :rb
        emit line, :pp
      end
    end

    def emit line, state
      if state == :rb
        if @last_emitted == :text
          @ruby_output << "]; #{line}"
          @last_emitted = :code
        else
          @ruby_output << line
        end
      else # state :pp
        if @last_emitted == :code
          @ruby_output << "target_out %[#{escape_code line}"
          @last_emitted = :text
        else
          @ruby_output << escape_code(line)
        end
      end
    end

    def antistate state
      (state == :pp) ? (:rb) : (:pp)
    end

    def escape_code str
      ruby_snippets = []
      # we don't want anything escaped within ruby snippets, so first we move them out
      str.gsub!(@TICKS) { ruby_snippets << $1; "%%#{ruby_snippets.size - 1}" }
      # after escaping what's necessary, we move them back in
      str.gsub('\\', '\\\\\\').gsub(']', '\\]').gsub('[', '\\[').gsub(/%%(\d+)/) { "\#{#{ruby_snippets[ $1.to_i ]}}" }
    end

    def pprb_include_dirs
      return @pprb_include_dirs if @pprb_include_dirs != nil
      return [] if @path == nil

      dir = File.dirname @path
      dirs = []

      while true
        dirs << dir

        modules_dir = dir + "/pprb_modules"
        dirs << modules_dir if File.directory? modules_dir

        break if dir == '/' or dir == '/cygdrive'

        parent_dir = File.dirname dir
        break if parent_dir == dir # this a terminating condition on Windows

        dir = parent_dir
      end

      @pprb_include_dirs = dirs.reverse
    end

    def load_rules rules_file
      pprb_include_dirs.each do |dir|
        next unless File.exist?(rules = "#{dir}/#{rules_file}")
        open(rules) { |f| run_and_translate_errors(f.read, rules) }
      end
    end
    
    def use file
      found = false

      pprb_include_dirs.each do |dir|
        next unless File.exist?(include = "#{dir}/#{file}.pprb.i")
        found = true
        open(include) do |file|
          pprb = PPRB.new(file.read, :rb)
          run_and_translate_errors pprb.ruby_output, include
        end
      end

      raise "Could not find PPRB module '#{file}'" unless found
    end

    def target_out text
      @target_output << text
    end

    def run_and_translate_errors code, filename = @path
      instance_eval code
    rescue RuntimeError, Exception => err
      source_line = nil
      p err
      err.backtrace.grep(/\(eval\):(\d+)/) { source_line ||= $1.to_i }

      if source_line
        $stderr.puts "#{filename}:#{source_line}: error: #{err.to_s}"
        $stderr.puts

        $stderr.puts "Ruby backtrace:"
        $stderr.puts err.backtrace

        $stderr.puts "Working directory: #{Dir.pwd}"
      else
        raise err
      end
    end
  end

end

