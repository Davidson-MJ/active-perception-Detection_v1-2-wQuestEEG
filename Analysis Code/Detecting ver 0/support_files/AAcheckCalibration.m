%quick calibration check:

%quickly load raw csv (summary), and plot the results of calibration.

%%
%%%%%% QUEST & EEG version

% Detection experiment (contrast)
%%  Import from csv. FramebyFrame, then summary data.

%frame by frame first:
%PC
datadir='C:\Users\User\Documents\matt\GitHub\active-perception-Detection_v1-2-wQuestEEG\Analysis Code\Detecting ver 0\Raw_data';
  
cd(datadir)
pfols = dir([pwd filesep '*trialsummary.csv']);
nsubs= length(pfols);
tr= table([1:length(pfols)]',{pfols(:).name}' );
disp(tr)

%
%% Per csv file, import and wrangle into Matlab Structures, and data matrices:
for ippant =2:length(pfols)
  
cd(datadir)
pfols = dir([pwd filesep '*trialsummary.csv']);
nsubs= length(pfols);
%
filename = pfols(ippant).name;
  
    %extract name&date from filename:
    ftmp = find(filename =='_');
    subjID = filename(1:ftmp(end)-1);
    %read table
    opts = detectImportOptions(filename,'NumHeaderLines',0);
    T = readtable(filename,opts);
    rawSummary_table = T;
    disp(['Preparing participant ' T.participant{1} ]);
    %%
    
    savename = [subjID '_summary_data'];
    % summarise relevant data:
    % calibration is performed after every dual target presented
    targPrestrials =find(T.nTarg>0);
    practIndex = find(T.isPrac ==1);
    nPracBlocks = unique(T.block(practIndex));
    nPrac=length(nPracBlocks);
    ss= size(practIndex,1);
    practIndex(ss+1) = practIndex(ss)+1;
    
    %% repair staircase allocation (freezing at start/end of blocks)
%     evens = find(mod(T.trial,2)==0);
%     odds =   find(mod(T.trial,2)~=0);
%     
%     T.qStaircase(evens)=1;
%     T.qStaircase(odds)=2;
    %%
    disp([subjID ' has ' num2str((T.trial(practIndex(end)) +1)) ' practice trials']);
    %extract the rows in our table, with relevant data for assessing
    %calibration:
    cols= {'r', 'b', 'k'};
    lg=[];
     %% figure 1
    figure(1);  clf; 
    set(gcf, 'color', 'w', 'units', 'normalized', 'position', [0 0 .5 .5]);
    hold on;
   nstairs = unique(T.qStaircase);
   nstairs = nstairs(nstairs>0);
   %%
   pcounter=1;
   for ipracblock = 1:nPrac
    for iqstair=1:length(nstairs)% check all staircases.
        usestair= nstairs(iqstair);
        qstairtrials= find(T.qStaircase==usestair);
        blckindx = find(T.block == nPracBlocks(ipracblock));
        calibAxis = intersect(qstairtrials, blckindx);
    
    %calculate accuracy:
    calibData = T.targCor(calibAxis);
    calibAcc = zeros(1, length(calibData));
    for itarg=1:length(calibData)
        tmpD = calibData(1:itarg);
        calibAcc(itarg) = sum(tmpD)/length(tmpD);
    end
    
    %retain contrast values:
    calibContrast = T.targContrast(calibAxis);
    % also show the contrast used after staircase:
    exprows = find(T.isPrac==0);
    exprows=exprows(2:end); % remove first target out of staircase.
    expContrasts= unique(T.targContrast(exprows));
    
 %%
subplot(nPrac,2,pcounter);    
plot(calibContrast, 'o-', 'color', cols{iqstair});
title('contrast'); hold on; ylabel('contrast')
xlabel('target count');
% add the final contr values:
for ic= 1:length(expContrasts)
plot(xlim, [expContrasts(ic), expContrasts(ic)], ['r:']);
end

subplot(nPrac,2,pcounter+1);
lg(iqstair) = plot(calibAcc, 'o-', 'color', cols{iqstair}); title('Accuracy');
hold on; ylim([0 1])
xlabel('target count');
title(['Block ' num2str(ipracblock)]) 

    end
    pcounter=pcounter+2;
   end
   
   %%
cd([datadir filesep 'Figures' filesep 'Calibration'])
    print('-dpng', [subjID ' quick summary'])
    %%
    
    %%
%     legend(lg, 'autoupdate', 'off');
    
    %% now plot accuracy standing and walking.
    %find standing and walking trials.
    strows= find(T.isStationary==1);
    wkrows= find(T.isStationary==0);
   
    %intersect of experiment, and relevant condition:
    standingrows = intersect(strows, exprows);
    walkingrows = intersect(wkrows, exprows);
    stblockIDs= find(diff(standingrows)>1);
    wkblockIDs= find(diff(wkrows)>1);
    %%
    standingAcc = sum(T.targCor(standingrows))/ length(standingrows);
   
    walkAcc = sum(T.targCor(walkingrows))/ length(walkingrows);
    
    
    %% also plot the result, just for the central staircase:
    stairResult = find(T.targContrast== expContrasts(4));
    midStair_standrows = intersect(stairResult, standingrows);
     midStair_walkrows = intersect(stairResult, walkingrows);

     midstandAcc =  sum(T.targCor(midStair_standrows))/ length(midStair_standrows);
     midwalkAcc  =  sum(T.targCor(midStair_walkrows))/ length(midStair_walkrows);

   %%
    figure(2);  clf; 
    set(gcf, 'color', 'w', 'units', 'normalized', 'position', [0 0 .5 .5]);
   
    subplot(131)
    bar([standingAcc, walkAcc])
    hold on;
    plot(1, midstandAcc, 'r-o')
    plot(2, midwalkAcc, 'r-o')
    
    shg
    %%
    title(subjID);
    set(gca,'xticklabels', {'standing', 'walking'})
    ylabel('meanAccuracy');
    ylim([.2 1]);
    hold on;
        
    text(1-.25, standingAcc*1.05, [num2str(round(standingAcc,2))]);
    text(2-.25, walkAcc*1.05, [num2str(round(walkAcc,2))]);
    
    %% also RT
    allRTs= T.targRT - T.targOnset;
    %remove negatives(these were no resp).
    allRTs(allRTs<0) = nan;
    
     standingRT = nansum(allRTs(standingrows))/ length(standingrows);   
     walkRT = nansum(allRTs(walkingrows))/ length(walkingrows);
    
    subplot(132)
    bar([standingRT, walkRT], 'Facecolor', 'r')
    shg
    title(subjID);
    set(gca,'xticklabels', {'standing', 'walking'})
    ylabel('meanRT');
    
    text(1-.25, standingRT*1.05, [num2str(round(standingRT,2))]);
    text(2-.25, walkRT*1.05, [num2str(round(walkRT,2))]);
    
    %% 
    % plot fits
    ret=pwd;
    % note for LT, the contrast logs are on desktop
    if strcmp(subjID(1:4),'LT01')
        %prep log.
        cd('C:\Users\User\Desktop');
        s =fileread('LT01log.txt');
        [tokens]= regexp(s, 'value is (0.\d*)', 'tokens');
        % disp(s(st(1):send(1)))
        %convert to array:
        nmat = [];
        format short
        for icon= 1:length(tokens)
            nmat(icon) = str2double(tokens{icon});            
        end
        % now replace the contrast value in table with these correct ones.
        T.targContrast(exprows) = nmat;
   cd(ret);
    end 
    
%     %% plot PF for standing and walking:
%     % now we can extract the types correct for each.
%     %whole exp first:
%%     exprows=standingrows;
% figure()
%%
leg=[];
for id=1:3
    switch id
        case 1
            userows= exprows;
            col= [.2 .2 .2];
        case 2
            userows = walkingrows;
            col = [0 .7 0];
        case 3
            userows= standingrows;
            col = [.7 0 0];
    end
%     %for all exp rows, get the counts per contrast type.
     StimLevelsall = T.targContrast(userows);
     responseall = T.targCor(userows);
%      %Each entry of StimLevelsall corresponds to single trial
     OutOfNum = ones(1,size(StimLevelsall,1));
%% 
%      %The following groups identical trials together
% %Before this line 'StimList' will have as many rows as there are trials, 
% %after this line, 'StimList' will have as many rows as there are unique 
% %stimuli. NumPos will contain the number of positive responses at each 
% %trial type, OutOfNum will contain the total number of trials at which the 
% %trial type was presented
[StimList, NumPos, OutOfNum] = PAL_MLDS_GroupTrialsbyX(StimLevelsall, responseall,...
    OutOfNum);  
%% avoid infinte values.
zerolist = find(NumPos==0);
NumPos(zerolist)= .001;
% %%
% %Parameter grid defining parameter space through which to perform a
% %brute-force search for values to be used as initial guesses in iterative
% %parameter search.
PF = @PAL_Logistic; 
searchGrid.alpha = StimList(1):.001:StimList(end);
searchGrid.beta = logspace(0,3,101);
searchGrid.gamma = 0.0;  %scalar here (since fixed) but may be vector
searchGrid.lambda = 0.02;  %ditto

%Threshold and Slope are free parameters, guess and lapse rate are fixed
paramsFree = [1 1 0 0];  %1: free parameter, 0: fixed parameter
 
[paramsValues LL exitflag] = PAL_PFML_Fit(StimList,NumPos, ...
    OutOfNum,searchGrid,paramsFree,PF);

disp('done:')
message = sprintf('Threshold estimate: %6.4f',paramsValues(1));
disp(message);
message = sprintf('Slope estimate: %6.4f\r',paramsValues(2));
disp(message);
%% plot"

%Create simple plot
ProportionCorrectObserved=NumPos./OutOfNum; 
StimLevelsFineGrain=[min(StimList):max(StimList)./1000:max(StimList)];
ProportionCorrectModel = PF(paramsValues,StimLevelsFineGrain);
 subplot(133);
title('MaxLL PF');
% axes
hold on
leg(id)=plot(StimLevelsFineGrain,ProportionCorrectModel,'-','color',col,'linewidth',4);
plot(StimList,ProportionCorrectObserved,['.'],'color', col,'markersize',40);
set(gca, 'fontsize',12);
set(gca, 'Xtick',StimList);
% axis([min(StimLevels) max(StimLevels) .4 1]);
xlabel('Stimulus Intensity');

ylabel('proportion correct');
hold on
end
legend(leg,{'exp', 'walking', 'standing'}, 'Location','SouthEast')
%%
cd([datadir filesep 'Figures' filesep 'Calibration'])
    print('-dpng', [subjID ' quick summary2'])
    
end % participant
