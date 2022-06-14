% plotj1_wholetrialGrandAverage

% using the whole trial information (no gait-cycle splitting), plot
% the accumulated average head trajectory, location of all H, M, and FA


cd([datadir filesep 'ProcessedData'])

pfols = dir([pwd filesep '*summary_data.mat']);
nsubs= length(pfols);
% nPrac=21;
%%
clf;
for ippant=1%:length(pfols)
    %%
    
    cd([datadir filesep 'ProcessedData'])
    load(pfols(ippant).name);
    %%
    %extract mean Head Y data
    HeadYAv= nan(size(HeadPos,2), 1000);
    ic=1;
    maxlength=1;
    
    for itrial= 1:size(HeadPos,2)
    
        if HeadPos(itrial).isStationary
            continue
        else
            HeadYtmp = HeadPos(itrial).Y;
            HeadYAv(ic,1:length(HeadYtmp))= HeadYtmp;
            
            if length(HeadYtmp)>maxlength
                maxlength = length(HeadYtmp);
                maxtrial = itrial;
            end
            ic=ic+1;
        end
        
    end
    % note some trials are longer than others, take the max length
    % across trials.
    time_ax = HeadPos(maxtrial).times';
   
    %% compare to tOnsets in t summary:
    tOnsets_smry = [];
    tOnsets_Hit_smry = [];
    tOnsets_Miss_smry = [];
    tOnsets_FA_smry = [];
    
    for itrial = 1:length(trial_TargetSummary)
        if trial_TargetSummary(itrial).isPrac
            continue
        end
        % all onsets:
        tO = trial_TargetSummary(itrial).targOnsets;
        tOnsets_smry = [tOnsets_smry, tO'];
        % all FAs:
        tF= trial_TargetSummary(itrial).FalseAlarms;
        if iscell(tF)
            tF= cell2mat(tF);
        end        
        tOnsets_FA_smry = [tOnsets_FA_smry, tF];
        % split by H and Miss:
        hm = find( trial_TargetSummary(itrial).targdetected);
        hits= tO(hm);
        tm =  find( trial_TargetSummary(itrial).targdetected ==0);
        misses = tO(tm);
        
        tOnsets_Hit_smry = [tOnsets_Hit_smry, hits'];
        tOnsets_Miss_smry = [tOnsets_Miss_smry, misses'];
        
    end
    %%
    %remove "-1" this is a targ absent place holder.
    tOnsets_smry = tOnsets_smry(tOnsets_smry>0);
    tOnsets_Hit_smry = tOnsets_Hit_smry(tOnsets_Hit_smry>0);
    tOnsets_Miss_smry = tOnsets_Miss_smry(tOnsets_Miss_smry>0);
    tOnsets_FA_smry = tOnsets_FA_smry(tOnsets_FA_smry>0);
    
    nTrials = size(HeadYAv,1);
    nTargs = length(tOnsets_smry);
    
    %adjust head data 
    HeadPlot = HeadYAv(:, 1:maxlength);
    stEH = CousineauSEM(HeadPlot);
    
   
    % plot
    figure(1);clf;
    set(gcf, 'units' ,'normalized', 'position', [0 0 .5 .5]);
    
    % plot Head (mean)
    shadedErrorBar(time_ax, nanmean(HeadPlot,1), stEH, 'k');
    
    
    ylabel('Head height (m)')
    xlabel('Time (sec)');
    title(['Grand mean, nWalk(' num2str(nTrials) '), nTargs(' num2str(nTargs) ') -' ppant])
    %%
    yyaxis right
    %     hg=histogram(tOnsets_smry, 100);
    % Hits / Miss split
    hH= histogram(tOnsets_Hit_smry, 100, 'Facecolor', [.2 1 .2]); hold on;
    hM=histogram(tOnsets_Miss_smry, 100, 'FaceColor', [1, .2 .2], 'BinWidth', hH.BinWidth); hold on;
    
    if ~isempty(tOnsets_FA_smry)
    hFA= histogram(tOnsets_FA_smry, 100, 'FaceColor', 'b', 'BinWidth', hH.BinWidth); hold on;
     legend([hH, hM, hFA], ...
        {['Hit:' num2str(length(tOnsets_Hit_smry))],...
        ['Miss: ' num2str(length(tOnsets_Miss_smry))],...
        ['FA: ' num2str(length(tOnsets_FA_smry))]}, 'location', 'NorthWest')
    else
          legend([hH, hM], ...
        {['Hit:' num2str(length(tOnsets_Hit_smry))],...
        ['Miss: ' num2str(length(tOnsets_Miss_smry))]},...
        'location', 'NorthWest')
    end
    ylabel('Target count')
   
    set(gca, 'fontsize', 15)
    %%
    cd([datadir filesep 'Figures' filesep 'Whole trial plot'])
    
    print('-dpng', [subjID  ' whole trial summary'])
end